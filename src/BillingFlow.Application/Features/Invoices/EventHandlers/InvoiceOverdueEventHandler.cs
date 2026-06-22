using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Invoices.IntegrationEvents;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Invoices.EventHandlers;

/// <summary>
/// Reacts to an internal domain InvoiceOverdueEvent.
/// Coordinates read-side lookups and pushes a technical Integration Event into the Outbox pipeline
/// to guarantee at-least-once notification delivery without violating boundary context transaction safety.
/// </summary>
public class InvoiceOverdueEventHandler(
    ILogger<InvoiceOverdueEventHandler> logger,
    IApplicationDbContext context,
    IIntegrationEventPublisher eventPublisher) : INotificationHandler<InvoiceOverdueEvent>
{
    public async Task Handle(InvoiceOverdueEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Domain Event Triggered: Invoice {InvoiceId} is overdue.", notification.InvoiceId);

        // Fetch necessary routing details using a single optimized database roundtrip (LINQ Joins)
        var payload = await (
            from i in context.Invoices.AsNoTracking()
            join c in context.Clients.AsNoTracking() on i.ClientId equals c.Id
            join u in context.Users.AsNoTracking() on c.UserId equals u.Id
            where i.Id == notification.InvoiceId && c.UserId != null
            select new
            {
                u.Email,
                c.CompanyName,
                i.InvoiceNumber
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (payload is null)
        {
            logger.LogWarning("Cannot send overdue notice. Client for Invoice {InvoiceId} has no associated AppUser or email.", notification.InvoiceId);
            return;
        }

        // We capture the data snapshot
        // and delegate delivery reliability to the infrastructural transactional integration outbox log.
        eventPublisher.Publish(
            new SendInvoiceOverdueNoticeEvent(
                payload.Email,
                payload.CompanyName,
                payload.InvoiceNumber,
                notification.AmountDue));
    }
}
