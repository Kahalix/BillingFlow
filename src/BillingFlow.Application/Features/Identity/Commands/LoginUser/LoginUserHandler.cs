// File: src/BillingFlow.Application/Features/Identity/Commands/LoginUser/LoginUserHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.LoginUser;

/// <summary>
/// Handles the authentication process, validates credentials, and issues JWT and Refresh tokens.
/// </summary>
public class LoginUserHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    ITokenHashService tokenHashService,
    TimeProvider timeProvider)
    : IRequestHandler<LoginUserCommand, AuthTokensResponse>
{
    public async Task<AuthTokensResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Normalize input for querying
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 2. Retrieve the user by email
        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Security best practice: Uniform exception to prevent account enumeration
        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        // 3. Validate Domain Invariants (User Status)
        if (user.Status != UserStatus.Active)
        {
            throw new ForbiddenException("This account is currently suspended or deactivated.");
        }

        // 4. Generate a unique Session ID and Tokens
        var sessionId = Guid.NewGuid();

        // Correctly using the ITokenGenerator contract
        var accessToken = tokenGenerator.GenerateJwt(user, sessionId);
        var refreshTokenString = tokenGenerator.GenerateSecureToken();

        // Deterministic hashing for database lookup during token rotation
        var hashedRefreshToken = tokenHashService.HashToken(refreshTokenString);

        // 5. Create the Refresh Token Domain Entity (Valid for 7 days)
        var tokenExpiry = timeProvider.GetUtcNow().AddDays(7);
        var userToken = new UserToken(
            userId: user.Id,
            sessionId: sessionId,
            type: UserTokenType.RefreshToken,
            tokenHash: hashedRefreshToken,
            expiry: tokenExpiry
        );

        // 6. Update Aggregate State
        user.RecordLogin(timeProvider.GetUtcNow());

        // 7. Persist Changes
        context.UserTokens.Add(userToken);
        await context.SaveChangesAsync(cancellationToken);

        // 8. Return the Payload
        return new AuthTokensResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenString,
            ExpiresAt: tokenExpiry
        );
    }
}
