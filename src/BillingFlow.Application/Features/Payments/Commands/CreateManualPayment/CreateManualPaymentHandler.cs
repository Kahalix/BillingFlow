using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Payments.Commands.CreateManualPayment;

public class CreateManualPaymentHandler(
    IApplicationDbContext context,
    TimeProvider timeProvider,
    ICurrentUserService currentUserService) : ICommandHandler<CreateManualPaymentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateManualPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the Aggregate Root
        var invoice = await context.Invoices
            .SingleOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
            return Result.Failure<Guid>(Errors.Payments.InvoiceNotFound(request.InvoiceId));

        var now = timeProvider.GetUtcNow();

        // 2. Delegate business rules to the Domain (Invoice status transitions, balance checks, events)
        // If the amount is higher than the remaining debt, this will safely throw a DomainException.
        invoice.ApplyPayment(request.Amount);

        // 3. Create the permanent accounting ledger record
        // The CreateManualPayment domain method inherently checks if request.Method.IsOfflineMethod().
        var payment = Payment.CreateManualPayment(
            invoiceId: invoice.Id,
            clientId: invoice.ClientId,
            amount: request.Amount,
            method: request.Method,
            paymentDate: request.PaymentDate,
            receivedByUserId: currentUserService.UserId, // Full audit traceability
            notes: request.Notes,
            now: now
        );

        context.Payments.Add(payment);

        // 4. Commit transaction
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(payment.Id);
    }
}
