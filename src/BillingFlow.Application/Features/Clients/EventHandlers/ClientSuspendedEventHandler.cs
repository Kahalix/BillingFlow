using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Clients.EventHandlers;

/// <summary>
/// Reacts to a client being suspended in the domain.
/// Dispatches a delayed background job to notify the client via email.
/// </summary>
public class ClientSuspendedEventHandler(
    ILogger<ClientSuspendedEventHandler> logger,
    IApplicationDbContext context,
    IBackgroundJobClient backgroundJobs) : INotificationHandler<ClientSuspendedEvent>
{
    public async Task Handle(ClientSuspendedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Domain Event Triggered: Client {ClientId} was suspended.", notification.ClientId);

        // Fetch necessary routing details using a single optimized database roundtrip (LINQ Join)
        var payload = await (
            from c in context.Clients.AsNoTracking()
            join u in context.Users.AsNoTracking() on c.UserId equals u.Id
            where c.Id == notification.ClientId && c.UserId != null
            select new
            {
                u.Email,
                c.CompanyName
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (payload is null)
        {
            logger.LogWarning("Cannot send suspension notice. Client {ClientId} has no associated AppUser or email.", notification.ClientId);
            return;
        }

        // PRAGMATIC TRADE-OFF: Hardcoding a 1-minute delay to mitigate the "publish-before-commit" risk. TODO
        // This gives the database transaction ample time to commit the 'Suspended' status before the email is dispatched.
        // This is a temporary safeguard and should be replaced by the Transactional Outbox pattern in the future.
        backgroundJobs.Schedule<IEmailSender>(sender =>
            sender.SendClientSuspensionNoticeAsync(
                payload.Email,
                payload.CompanyName,
                default),
            TimeSpan.FromMinutes(1));
    }
}
