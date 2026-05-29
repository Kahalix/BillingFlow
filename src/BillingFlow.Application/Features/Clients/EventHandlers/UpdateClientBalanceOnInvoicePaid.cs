// File: src/BillingFlow.Application/Features/Clients/ReadModels/EventHandlers/UpdateClientBalanceOnInvoicePaid.cs
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

namespace BillingFlow.Application.Features.Clients.ReadModels.EventHandlers;

public class UpdateClientBalanceOnInvoicePaid(
    IClientBalanceProjectionWriter projectionWriter,
    TimeProvider timeProvider) : INotificationHandler<InvoicePaidEvent>
{
    public async Task Handle(InvoicePaidEvent notification, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        // Negative delta -> decreases debt
        await projectionWriter.ApplyDebtDeltaAsync(notification.ClientId, -notification.TotalInvoiceAmount, now, cancellationToken);

        // TODO: Later, we can inject an ISignalRClient to push a real-time notification to the frontend

    }
}
