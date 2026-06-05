using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

namespace BillingFlow.Application.Features.Invoices.EventHandlers;

public class RevertDebtOnInvoiceCanceled(
    IClientBalanceProjectionWriter projectionWriter,
    TimeProvider timeProvider) : INotificationHandler<InvoiceCanceledEvent>
{
    public async Task Handle(InvoiceCanceledEvent notification, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        // Negative delta completely rolls back the debt from the customer's balance view
        await projectionWriter.ApplyDebtDeltaAsync(notification.ClientId, -notification.TotalAmount, now, cancellationToken);
    }
}
