using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Payments.Commands.ProcessPaymentCompleted;

public class ProcessPaymentCompletedHandler(
    IApplicationDbContext context,
    TimeProvider timeProvider,
    ILogger<ProcessPaymentCompletedHandler> logger) : IRequestHandler<ProcessPaymentCompletedCommand>
{
    public async Task Handle(ProcessPaymentCompletedCommand request, CancellationToken cancellationToken)
    {
        // 1. COMPOSITE KEY SEARCH: Locate the attempt by Provider + Reference
        // Protects against reference overlapping across different payment providers (e.g. PayPal vs Stripe)
        var attempt = await context.PaymentAttempts
            .SingleOrDefaultAsync(pa =>
                pa.Provider == PaymentProvider.Stripe &&
                pa.ProviderReference == request.ProviderReference,
                cancellationToken);

        if (attempt == null)
        {
            logger.LogWarning("Webhook received for an unknown payment attempt (Provider: Stripe, Reference: {Reference})", request.ProviderReference);
            return;
        }

        // Memory-level Idempotency Check: Fast pass if already locally modified
        if (attempt.Status == PaymentStatus.Succeeded)
        {
            logger.LogInformation("Payment attempt {AttemptId} was already marked as succeeded. Ignoring duplicate webhook.", attempt.Id);
            return;
        }

        // 2. FETCH LINKED AGGREGATE ROOT
        var invoice = await context.Invoices
            .SingleOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
        {
            logger.LogCritical("Payment attempt succeeded, but related Invoice {InvoiceId} is missing from the database!", request.InvoiceId);
            return;
        }

        var now = timeProvider.GetUtcNow();

        // 3. EXECUTE DOMAIN STATE MUTATIONS
        attempt.MarkAsSucceeded();
        invoice.ApplyPayment(attempt.Amount);

        // 4. GENERATE PERMANENT ACCOUNTING LEDGER (Payment Document)
        // Financial records must be decoupled from attempts for compliance and audatability.
        var payment = Payment.CreateOnlinePayment(
            invoiceId: invoice.Id,
            clientId: invoice.ClientId,
            paymentAttemptId: attempt.Id,
            amount: attempt.Amount,
            provider: PaymentProvider.Stripe,
            method: request.Method,
            externalTransactionId: request.ProviderReference,
            paymentDate: now,
            now: now
        );

        context.Payments.Add(payment);

        // 5. STAGE IDEMPOTENCY LOG RECORD
        // This will be saved in the exact same transaction, fulfilling strict set-based consistency rules.
        var eventLog = new StripeEventLog(request.EventId, now);
        context.StripeEventLogs.Add(eventLog);

        // 6. ATOMIC SAVE CHANGES (ALL OR NOTHING TRANSACTION BOUNDARY)
        try
        {
            // EF Core implicitly wraps this block into a single database transaction.
            // If the unique index on StripeEventLog.EventId fails, the entire stack (Attempt, Invoice, Payment) is rolled back.
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully completed atomic payment processing ledger for Invoice {InvoiceId}. Event {EventId} archived.", invoice.Id, request.EventId);
        }
        catch (UniqueConstraintException ex) when (ex.EntityName == nameof(StripeEventLog))
        {
            // Graceful recovery from race conditions (Stripe network concurrent retry delivery).
            // We only swallow the exception if it was explicitly the idempotency log that clashed.
            // We log it and swallow the exception to return HTTP 200 OK to Stripe, signaling that the event is taken care of.
            logger.LogInformation("Concurrent webhook race condition detected for Stripe Event {EventId}. Duplicate transaction safely aborted and rolled back.", request.EventId);
        }
    }
}
