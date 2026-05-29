// File: src/BillingFlow.Application/Features/Invoices/EventHandlers/FreeProvidedServicesOnInvoiceCanceled.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Invoices.EventHandlers;

public class FreeProvidedServicesOnInvoiceCanceled(IApplicationDbContext context) : INotificationHandler<InvoiceCanceledEvent>
{
    public async Task Handle(InvoiceCanceledEvent notification, CancellationToken cancellationToken)
    {
        // High-Performance batch update using EF Core 8 native features
        await context.ProvidedServices
            .Where(ps => ps.InvoiceId == notification.InvoiceId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.InvoiceId, (Guid?)null)
                .SetProperty(p => p.BilledAt, (DateTimeOffset?)null),
                cancellationToken);
    }
}
