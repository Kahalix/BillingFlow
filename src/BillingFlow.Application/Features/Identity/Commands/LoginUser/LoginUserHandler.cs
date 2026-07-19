using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Identity;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillingFlow.Application.Features.Identity.Commands.LoginUser;

public class LoginUserHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    ITokenHashService tokenHashService,
    IRequestContext requestContext,
    IOptions<IdentityOptions> options,
    TimeProvider timeProvider) : ICommandHandler<LoginUserCommand, AuthTokensResponse>
{
    private readonly IdentityOptions _options = options.Value;
    
    public async Task<Result<AuthTokensResponse>> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            return Result.Failure<AuthTokensResponse>(Errors.Auth.InvalidCredentials);

        if (user.Status != UserStatus.Active)
            return Result.Failure<AuthTokensResponse>(Errors.Users.Suspended);

        var now = timeProvider.GetUtcNow();
        var sessionExpiry = now.Add(_options.SessionLifetime);
        var absoluteSessionExpiry = now.Add(_options.AbsoluteSessionLifetime);
        var proposedRefreshExpiry = now.Add(_options.RefreshTokenLifetime);
        var refreshTokenExpiry = proposedRefreshExpiry < absoluteSessionExpiry
            ? proposedRefreshExpiry
            : absoluteSessionExpiry;

        var session = new UserSession(
            user.Id,
            now,
            sessionExpiry,
            absoluteSessionExpiry,
            requestContext.IpAddress,
            requestContext.UserAgent);

        var accessToken = tokenGenerator.GenerateJwt(user, session.Id);
        var refreshTokenString = tokenGenerator.GenerateSecureToken();
        var hashedRefreshToken = tokenHashService.HashToken(refreshTokenString);

        var userToken = UserToken.CreateRefreshToken(user.Id, session.Id, hashedRefreshToken, now, refreshTokenExpiry);

        user.RecordLogin(now);
        context.UserSessions.Add(session);
        context.UserTokens.Add(userToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(new AuthTokensResponse(accessToken, refreshTokenString, refreshTokenExpiry));
    }
}
