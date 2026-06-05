using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;

public class CreatePaymentSessionHandler(
    IApplicationDbContext context,
    IStripeService stripeService,
    TimeProvider timeProvider,
    ILogger<CreatePaymentSessionHandler> logger) : IRequestHandler<CreatePaymentSessionCommand, PaymentSessionResponse>
{
    public async Task<PaymentSessionResponse> Handle(CreatePaymentSessionCommand request, CancellationToken cancellationToken)
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
                return new PaymentSessionResponse(activeAttempt.ProviderReference!, activeAttempt.CheckoutUrl!);
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

        if (invoice is null) throw new NotFoundException($"Invoice with ID {request.InvoiceId} could not be found.");
        if (invoice.Status == InvoiceStatus.Paid) throw new ForbiddenException("This invoice has already been fully paid.");
        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Canceled) throw new ForbiddenException("Payment sessions can only be created for active, unpaid invoices.");

        var amountDue = invoice.TotalAmount - invoice.PaidAmount;
        if (amountDue <= 0) throw new ForbiddenException("Amount due must be strictly greater than zero to initiate a payment.");

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
                // Graceful degradation only on true race conditions
                throw new DomainException("A payment session is already being created for this invoice. Please wait a moment and try again.");
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

            return new PaymentSessionResponse(providerReference, checkoutUrl);
        }
        catch (OperationCanceledException)
        {
            // Do not wrap or fail the reservation if the client simply disconnected.
            throw;
        }
        catch (ExternalServiceException ex) // <-- CLEAN ARCHITECTURE: Catching agnostic exception!
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
