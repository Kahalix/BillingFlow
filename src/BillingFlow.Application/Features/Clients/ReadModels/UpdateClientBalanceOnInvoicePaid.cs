// File: src/BillingFlow.Application/Features/Clients/ReadModels/EventHandlers/UpdateClientBalanceOnInvoicePaid.cs
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Clients.ReadModels.EventHandlers;

public class UpdateClientBalanceOnInvoicePaid(ILogger<UpdateClientBalanceOnInvoicePaid> logger) : INotificationHandler<InvoicePaidEvent>
{
    public Task Handle(InvoicePaidEvent notification, CancellationToken cancellationToken)
    {
        // Balances are strictly tied to Cash Flow events (PaymentRecordedEvent),
        // not status transitions. This handler is now reserved purely for operational side-effects.
        logger.LogInformation("Invoice {InvoiceId} is fully paid. (Operational hooks: e.g., send 'Thank you' email).", notification.InvoiceId);

        return Task.CompletedTask;

        // TODO: Later, we can inject an ISignalRClient to push a real-time notification to the frontend

    }
}
