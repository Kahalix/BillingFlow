using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.EventHandlers;

/// <summary>
/// Reacts to a user being suspended by an administrator.
/// Enforces immediate security by revoking all active refresh tokens for the suspended user.
/// </summary>
public class UserSuspendedEventHandler(
    IApplicationDbContext context,
    TimeProvider timeProvider)
    : INotificationHandler<UserSuspendedEvent>
{
    public async Task Handle(UserSuspendedEvent notification, CancellationToken cancellationToken)
    {
        // 1. Find all active tokens for the suspended user
        var activeTokens = await context.UserTokens
            .Where(t => t.UserId == notification.UserId && t.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        if (!activeTokens.Any()) return;

        // 2. Consume (invalidate) all of them
        foreach (var token in activeTokens)
        {
            token.MarkAsConsumed(timeProvider);
        }

        // 3. Persist the invalidations
        await context.SaveChangesAsync(cancellationToken);
    }
}
