using System;
using System.Threading;
using System.Threading.Tasks;
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.Logout;

public class LogoutHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ISessionCacheService sessionCacheService,
    TimeProvider timeProvider) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var currentSessionId = currentUserService.SessionId;
        
        if (currentSessionId == Guid.Empty)
            throw new UnauthorizedException("Invalid session context.");

        var now = timeProvider.GetUtcNow();
        var session = await context.UserSessions.FirstOrDefaultAsync(s => s.Id == currentSessionId, cancellationToken);

        if (session != null && session.IsActive(now))
        {
            session.Revoke(now);
            await context.SaveChangesAsync(cancellationToken);
            
            // Explicitly evict the cache to ensure API tokens tied to this session fail instantly
            await sessionCacheService.InvalidateSessionAsync(currentSessionId, cancellationToken);
        }
    }
}
