using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.LogoutAllDevices;

public class LogoutAllDevicesHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ISessionCacheService sessionCacheService,
    TimeProvider timeProvider) : ICommandHandler<LogoutAllDevicesCommand>
{
    public async Task<Result> Handle(LogoutAllDevicesCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var now = timeProvider.GetUtcNow();

        // 1. Fetch ALL active sessions (both API and Dashboard) for this user.
        // The domain source of truth is now UserSessions, not UserTokens.
        var activeSessions = await context.UserSessions
            .Where(s => s.UserId == currentUserId && s.RevokedAt == null && s.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        if (!activeSessions.Any()) return Result.Success();

        // 2. Revoke durably in the database
        foreach (var session in activeSessions)
        {
            session.Revoke(now);
        }

        await context.SaveChangesAsync(cancellationToken);

        // 3. Invalidate distributed cache entries so all nodes observe the revoked session.
        await Task.WhenAll(
            activeSessions.Select(session =>
                sessionCacheService.InvalidateSessionAsync(session.Id, cancellationToken))
        );

        return Result.Success();
    }
}
