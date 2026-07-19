using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;

public class CreatePaymentSessionHandler(
    IApplicationDbContext context,
    IStripeService stripeService,
    TimeProvider timeProvider,
    ILogger<CreatePaymentSessionHandler> logger) : ICommandHandler<CreatePaymentSessionCommand, PaymentSessionResponse>
{
    public async Task<Result<PaymentSessionResponse>> Handle(CreatePaymentSessionCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        PaymentAttempt? reservationToProcess = null;

        // 1. CLEAR STALE ATTEMPTS & CHECK LOCAL IDEMPOTENCY
        var activeAttempt = await context.PaymentAttempts
            .Where(pa => pa.InvoiceId == request.InvoiceId &&
                        (pa.Status == PaymentStatus.Started || pa.Status == PaymentStatus.Initializing))
            .SingleOrDefaultAsync(cancellationToken);

        if (activeAttempt != null)
        {
            // Self-healing: expire dead attempts
            if (activeAttempt.ExpiresAt <= now)
            {
                activeAttempt.MarkAsExpired();
                await context.SaveChangesAsync(cancellationToken);
            }
            else if (activeAttempt.Status == PaymentStatus.Started)
            {
                // Fully active, return the existing URL (Idempotency)
                return Result.Success(new PaymentSessionResponse(activeAttempt.ProviderReference!, activeAttempt.CheckoutUrl!));
            }
            else if (activeAttempt.Status == PaymentStatus.Initializing)
            {
                // The attempt is alive but stuck in Initializing.
                // We reuse this reservation and re-send the Idempotency Key to the provider to recover the lost URL!
                reservationToProcess = activeAttempt;
            }
        }

        // 2. FETCH AND VALIDATE INVOICE
        var invoice = await context.Invoices
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(i => i.Id == request.InvoiceId)
            .Select(i => new { i.Id, i.Status, i.TotalAmount, i.PaidAmount })
            .SingleOrDefaultAsync(cancellationToken);

        if (invoice is null) return Result.Failure<PaymentSessionResponse>(Errors.Payments.InvoiceNotFound(request.InvoiceId));
        if (invoice.Status == InvoiceStatus.Paid) return Result.Failure<PaymentSessionResponse>(Errors.Payments.InvoiceFullyPaid);
        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Canceled) return Result.Failure<PaymentSessionResponse>(Errors.Payments.InvoiceNotActive);

        var amountDue = invoice.TotalAmount - invoice.PaidAmount;
        if (amountDue <= 0) return Result.Failure<PaymentSessionResponse>(Errors.Payments.InvalidAmount);

        var sessionExpiry = now.AddHours(24).AddMinutes(-5);

        // 3. PHASE 1: DATABASE RESERVATION WITH CONCURRENCY LOCK HANDLING
        if (reservationToProcess == null)
        {
            reservationToProcess = PaymentAttempt.Reserve(invoice.Id, amountDue, PaymentProvider.Stripe, now, sessionExpiry);
            context.PaymentAttempts.Add(reservationToProcess);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (UniqueConstraintException)
            {
                return Result.Failure<PaymentSessionResponse>(Errors.Payments.SessionRaceCondition);
            }
        }

        // 4. PHASE 2: EXTERNAL API CALL WITH IDEMPOTENCY KEY
        try
        {
            var (providerReference, checkoutUrl) = await stripeService.CreateCheckoutSessionAsync(
                invoice.Id,
                amountDue,
                request.SuccessUrl,
                request.CancelUrl,
                reservationToProcess.Id.ToString(), // Idempotency Key
                cancellationToken);

            // Successfully received URL -> Transition to 'Started'
            reservationToProcess.SetCheckoutDetails(providerReference, checkoutUrl);
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success(new PaymentSessionResponse(providerReference, checkoutUrl));
        }
        catch (OperationCanceledException)
        {
            // Do not wrap or fail the reservation if the client simply disconnected.
            throw;
        }
        catch (ExternalServiceException ex)
        {
            // SECURE LOGGING: Write raw, potentially sensitive API errors to internal logs only
            logger.LogError(ex, "Payment gateway integration failed for Invoice {InvoiceId}. Transient: {IsTransient}. Reason: {Message}",
                invoice.Id, ex.IsTransient, ex.Message);

            // We failed to get a link due to a real provider rejection.
            // Explicitly fail the DB reservation to free the lock and clean up ONLY if the error is definitive.
            if (!ex.IsTransient)
            {
                reservationToProcess.MarkAsFailed("Payment initialization was rejected by the gateway.");
                await context.SaveChangesAsync(cancellationToken);
            }

            // Re-throw so the global exception handler can return HTTP 502 Bad Gateway
            throw;
        }
    }
}
