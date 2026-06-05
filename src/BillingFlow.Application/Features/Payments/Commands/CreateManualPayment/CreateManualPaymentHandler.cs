using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Payments.Commands.CreateManualPayment;

public class CreateManualPaymentHandler(
    IApplicationDbContext context,
    TimeProvider timeProvider,
    ICurrentUserService currentUserService) : IRequestHandler<CreateManualPaymentCommand, Guid>
{
    public async Task<Guid> Handle(CreateManualPaymentCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the Aggregate Root
        var invoice = await context.Invoices
            .SingleOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
            throw new NotFoundException($"Invoice with ID {request.InvoiceId} could not be found.");

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

        return payment.Id;
    }
}
