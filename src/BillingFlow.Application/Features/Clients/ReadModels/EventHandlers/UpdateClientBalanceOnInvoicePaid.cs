using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Clients.ReadModels.EventHandlers;

/// <summary>
/// Reacts to an invoice being fully paid. 
/// Reserved purely for operational side-effects (e.g., sending email receipts), not balance calculations.
/// </summary>
public class UpdateClientBalanceOnInvoicePaid(ILogger<UpdateClientBalanceOnInvoicePaid> logger) : INotificationHandler<InvoicePaidEvent>
{
    public Task Handle(InvoicePaidEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Invoice {InvoiceId} is fully paid. (Operational hooks: e.g., send 'Thank you' email).", notification.InvoiceId);
        return Task.CompletedTask;
    }
}
