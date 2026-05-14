// File: src/BillingFlow.Application/Features/Identity/Commands/RefreshSession/RefreshSessionHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Identity.Commands.LoginUser;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.RefreshSession;

/// <summary>
/// Handles token rotation with strict Replay Attack detection and Optimistic Concurrency protection.
/// </summary>
public class RefreshSessionHandler(
    IApplicationDbContext context,
    ITokenGenerator tokenGenerator,
    ITokenHashService tokenHashService,
    TimeProvider timeProvider)
    : IRequestHandler<RefreshSessionCommand, AuthTokensResponse>
{
    public async Task<AuthTokensResponse> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var incomingHash = tokenHashService.HashToken(request.RefreshToken);

        var existingToken = await context.UserTokens
            .Where(t => t.TokenHash == incomingHash && t.Type == UserTokenType.RefreshToken)
            .Join(context.Users,
                t => t.UserId,
                u => u.Id,
                (t, u) => new { Token = t, User = u })
            .SingleOrDefaultAsync(cancellationToken);

        if (existingToken is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var token = existingToken.Token;
        var user = existingToken.User;

        // 1. REPLAY ATTACK MITIGATION: Aggressive Session Revocation
        if (!token.IsActive(timeProvider))
        {
            // If the token is already consumed, the session is compromised.
            // We fetch and consume ALL active tokens tied to this specific SessionId.
            var activeSessionTokens = await context.UserTokens
                .Where(t => t.SessionId == token.SessionId && t.ConsumedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var activeToken in activeSessionTokens)
            {
                activeToken.MarkAsConsumed(timeProvider);
            }

            await context.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException("Session compromise detected. All active tokens for this session have been revoked. Please log in again.");
        }

        if (user.Status != UserStatus.Active)
        {
            throw new ForbiddenException("This account is currently suspended or deactivated.");
        }

        // 2. Consume the old token
        token.MarkAsConsumed(timeProvider);

        // 3. Generate new tokens
        var newAccessToken = tokenGenerator.GenerateJwt(user, token.SessionId);
        var newRefreshTokenString = tokenGenerator.GenerateSecureToken();
        var newHashedRefreshToken = tokenHashService.HashToken(newRefreshTokenString);

        var tokenExpiry = timeProvider.GetUtcNow().AddDays(7);
        var newUserToken = new UserToken(
            userId: user.Id,
            sessionId: token.SessionId,
            type: UserTokenType.RefreshToken,
            tokenHash: newHashedRefreshToken,
            expiry: tokenExpiry
        );

        context.UserTokens.Add(newUserToken);

        // 4. CONCURRENCY PROTECTION (Race Condition Guard)
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // If two requests attempt to refresh using the same token simultaneously,
            // the second one will hit this block because the RowVersion will have changed.
            // We treat this concurrent access as a Replay Attack attempt.
            throw new UnauthorizedAccessException("Session compromise detected (concurrent access). Please log in again.");
        }

        return new AuthTokensResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenString,
            ExpiresAt: tokenExpiry
        );
    }
}
