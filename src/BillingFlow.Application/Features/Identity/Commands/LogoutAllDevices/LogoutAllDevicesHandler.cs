// File: src/BillingFlow.Application/Features/Identity/Commands/LogoutAllDevices/LogoutAllDevicesHandler.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.LogoutAllDevices;

public class LogoutAllDevicesHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : IRequestHandler<LogoutAllDevicesCommand>
{
    public async Task Handle(LogoutAllDevicesCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;

        // Fetch ALL active REFRESH TOKENS for this user. 
        // We explicitly ignore PasswordReset or EmailConfirmation tokens.
        var allActiveTokens = await context.UserTokens
            .Where(t => t.UserId == currentUserId
                     && t.Type == UserTokenType.RefreshToken
                     && t.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        if (!allActiveTokens.Any()) return;

        foreach (var token in allActiveTokens)
        {
            token.MarkAsConsumed(timeProvider);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
