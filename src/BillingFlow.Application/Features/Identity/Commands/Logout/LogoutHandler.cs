// File: src/BillingFlow.Application/Features/Identity/Commands/Logout/LogoutHandler.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.Logout;

public class LogoutHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var currentSessionId = currentUserService.SessionId;

        if (currentSessionId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Invalid session context.");
        }

        // Fetch all tokens tied to this specific session that are still active
        var activeSessionTokens = await context.UserTokens
            .Where(t => t.SessionId == currentSessionId
                     && t.Type == UserTokenType.RefreshToken
                     && t.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        if (!activeSessionTokens.Any())
        {
            return; // Idempotency: Session is already dead, do nothing.
        }

        foreach (var token in activeSessionTokens)
        {
            token.MarkAsConsumed(timeProvider);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
