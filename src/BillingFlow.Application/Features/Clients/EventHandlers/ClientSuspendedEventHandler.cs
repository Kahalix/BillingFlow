using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Clients.IntegrationEvents;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Clients.EventHandlers;

/// <summary>
/// Reacts to a client being suspended within the domain.
/// Acts as a translation layer: it listens for internal Domain Events and translates them 
/// into external Integration Events via the Transactional Outbox.
/// </summary>
public class ClientSuspendedEventHandler(
    ILogger<ClientSuspendedEventHandler> logger,
    IApplicationDbContext context,
    IIntegrationEventPublisher eventPublisher) : INotificationHandler<ClientSuspendedEvent>
{
    public async Task Handle(ClientSuspendedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Domain Event Triggered: Client {ClientId} was suspended.", notification.ClientId);

        // Fetch necessary routing details using a single optimized database roundtrip (LINQ Join)
        var payload = await (
            from c in context.Clients.AsNoTracking()
            join u in context.Users.AsNoTracking() on c.UserId equals u.Id
            where c.Id == notification.ClientId && c.UserId != null
            select new { u.Email, c.CompanyName })
            .SingleOrDefaultAsync(cancellationToken);

        if (payload is null)
        {
            logger.LogWarning("Cannot send suspension notice. Client {ClientId} lacks an associated AppUser or email.", notification.ClientId);
            return;
        }

        // The publisher will serialize this intent into the current Entity Framework ChangeTracker, 
        // guaranteeing it is saved atomically with the Client's suspension status.
        eventPublisher.Publish(
            new SendClientSuspensionNoticeEvent(payload.Email, payload.CompanyName));
    }
}
