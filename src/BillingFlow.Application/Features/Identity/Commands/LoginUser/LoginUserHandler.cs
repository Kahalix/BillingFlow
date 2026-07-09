using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Identity;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using MediatR;

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
    TimeProvider timeProvider) : IRequestHandler<LoginUserCommand, AuthTokensResponse>
{
    private readonly IdentityOptions _options = options.Value;
    public async Task<AuthTokensResponse> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (user.Status != UserStatus.Active)
            throw new ForbiddenException("This account is currently suspended.");

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

        return new AuthTokensResponse(accessToken, refreshTokenString, refreshTokenExpiry);
    }
}
