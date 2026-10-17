using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Features.Identity.Commands.LoginUser;
using BillingFlow.Application.Identity;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillingFlow.Application.Features.Identity.Commands.RefreshSession;

public class RefreshSessionHandler(
    IApplicationDbContext context,
    ITokenGenerator tokenGenerator,
    ITokenHashService tokenHashService,
    ISessionCacheService sessionCacheService,
    IOptions<IdentityOptions> options,
    TimeProvider timeProvider) : ICommandHandler<RefreshSessionCommand, AuthTokensResponse>
{
    private readonly IdentityOptions _options = options.Value;
    // Throttling prevents database write-amplification caused by rapid token rotations
    private static readonly TimeSpan ActivityUpdateThreshold = TimeSpan.FromMinutes(5);

    public async Task<Result<AuthTokensResponse>> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        var incomingHash = tokenHashService.HashToken(request.RefreshToken);
        var now = timeProvider.GetUtcNow();

        var existingToken = await context.UserTokens
            .SingleOrDefaultAsync(t => t.TokenHash == incomingHash && t.Type == UserTokenType.RefreshToken, cancellationToken);

        if (existingToken is null || existingToken.SessionId is null)
        {
            return Result.Failure<AuthTokensResponse>(Errors.Auth.InvalidRefreshToken);
        }

        var session = await context.UserSessions.SingleOrDefaultAsync(s => s.Id == existingToken.SessionId, cancellationToken);
        var user = await context.Users.SingleOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken);

        if (session is null || user is null || user.Status != UserStatus.Active)
        {
            return Result.Failure<AuthTokensResponse>(Errors.Users.Suspended);
        }

        // Replay Attack Mitigation: Token was already consumed
        if (!existingToken.IsActive(now))
        {
            // Global session revocation
            session.Revoke(now);
            await context.SaveChangesAsync(cancellationToken);

            // Real-time eviction from distributed cache across all nodes
            await sessionCacheService.InvalidateSessionAsync(session.Id, cancellationToken);

            return Result.Failure<AuthTokensResponse>(Errors.Auth.SessionCompromised);
        }

        if (!session.IsActive(now))
        {
            return Result.Failure<AuthTokensResponse>(Errors.Auth.SessionExpired);
        }

        existingToken.MarkAsConsumed(now);

        // Throttle domain telemetry writes AND apply Sliding Expiration logic
        if (!session.LastSeenAt.HasValue || (now - session.LastSeenAt.Value) >= ActivityUpdateThreshold)
        {
            session.RefreshActivity(now, _options.SessionLifetime);
        }

        var proposedRefreshExpiry = now.Add(_options.RefreshTokenLifetime);
        var refreshTokenExpiry = proposedRefreshExpiry < session.ExpiresAt
            ? proposedRefreshExpiry
            : session.ExpiresAt;

        var newAccessToken = tokenGenerator.GenerateJwt(user, session.Id);
        var newRefreshTokenString = tokenGenerator.GenerateSecureToken();
        var newHashedRefreshToken = tokenHashService.HashToken(newRefreshTokenString);

        var newUserToken = UserToken.CreateRefreshToken(
            user.Id,
            session.Id,
            newHashedRefreshToken,
            now,
            refreshTokenExpiry);

        context.UserTokens.Add(newUserToken);

        // Optimistic concurrency execution protects against parallel use of the same refresh token
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<AuthTokensResponse>(Errors.Auth.ConcurrentModification);
        }

        return Result.Success(new AuthTokensResponse(newAccessToken, newRefreshTokenString, refreshTokenExpiry));
    }
}
