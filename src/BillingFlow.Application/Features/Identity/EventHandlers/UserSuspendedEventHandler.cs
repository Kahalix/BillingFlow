using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.EventHandlers;

public class UserSuspendedEventHandler(
    IApplicationDbContext context,
    ISessionCacheService sessionCacheService,
    TimeProvider timeProvider) : INotificationHandler<UserSuspendedEvent>
{
    public async Task Handle(UserSuspendedEvent notification, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // 1. Fetch all active sessions for the suspended user across all devices/clients.
        var activeSessions = await context.UserSessions
            .Where(s => s.UserId == notification.UserId && s.RevokedAt == null && s.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        if (!activeSessions.Any()) return;

        // 2. Durably revoke all sessions in the database.
        foreach (var session in activeSessions)
        {
            session.Revoke(now);
        }

        await context.SaveChangesAsync(cancellationToken);

        // 3. Invalidate distributed cache entries.
        // This ensures all application nodes observe the revoked state immediately.
        await Task.WhenAll(
            activeSessions.Select(session =>
                sessionCacheService.InvalidateSessionAsync(session.Id, cancellationToken))
        );
    }
}
