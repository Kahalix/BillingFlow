using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Clients.EventHandlers;

/// <summary>
/// Reacts to a client being suspended in the domain.
/// Dispatches a background job to notify the client via email.
/// </summary>
public class ClientSuspendedEventHandler(
    ILogger<ClientSuspendedEventHandler> logger,
    IApplicationDbContext context,
    IBackgroundJobClient backgroundJobs) : INotificationHandler<ClientSuspendedEvent>
{
    public async Task Handle(ClientSuspendedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Domain Event Triggered: Client {ClientId} was suspended.", notification.ClientId);

        // Fetch necessary details using a single optimized database roundtrip (LINQ Join)
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

        // Offload the email notification to Hangfire for reliable, asynchronous execution
        backgroundJobs.Enqueue<IEmailSender>(sender =>
            sender.SendClientSuspensionNoticeAsync(payload.Email, payload.CompanyName, CancellationToken.None));
    }
}
