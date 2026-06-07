using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

namespace BillingFlow.Application.Features.Clients.ReadModels.EventHandlers;

public class UpdateClientBalanceOnInvoiceGenerated(
    IClientBalanceProjectionWriter projectionWriter,
    TimeProvider timeProvider) : INotificationHandler<InvoiceGeneratedEvent>
{
    public async Task Handle(InvoiceGeneratedEvent notification, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        // Positive delta -> increases debt
        await projectionWriter.ApplyDebtDeltaAsync(notification.ClientId, notification.TotalAmount, now, cancellationToken);
    }
}
