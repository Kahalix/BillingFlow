using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.DashboardLogout;

public class DashboardLogoutHandler(
    IApplicationDbContext context,
    ISessionCacheService sessionCacheService,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider) : IRequestHandler<DashboardLogoutCommand>
{
    public async Task Handle(DashboardLogoutCommand request, CancellationToken cancellationToken)
    {
        var currentSessionId = currentUserService.SessionId;
        var currentUserId = currentUserService.UserId;

        if (currentSessionId == Guid.Empty)
            throw new UnauthorizedException("Invalid session context.");

        var now = timeProvider.GetUtcNow();

        var session = await context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == currentSessionId && s.UserId == currentUserId, cancellationToken);

        if (session != null && session.IsActive(now))
        {
            session.Revoke(now);
            await context.SaveChangesAsync(cancellationToken);
        }

        await sessionCacheService.InvalidateSessionAsync(currentSessionId, cancellationToken);
    }
}
