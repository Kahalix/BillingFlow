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

namespace BillingFlow.Application.Features.Identity.Commands.DashboardLogin;

public class DashboardLoginHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IRequestContext requestContext,
    IOptions<IdentityOptions> options,
    TimeProvider timeProvider) : IRequestHandler<DashboardLoginCommand, DashboardLoginResponse>
{
    private readonly IdentityOptions _options = options.Value;

    public async Task<DashboardLoginResponse> Handle(DashboardLoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (user.Status != UserStatus.Active)
            throw new ForbiddenException("This account is currently suspended.");

        if (user.Role != Role.Admin)
            throw new ForbiddenException("Access denied. Administrator privileges required.");

        var now = timeProvider.GetUtcNow();

        var sessionExpiry = now.Add(_options.SessionLifetime);
        var absoluteSessionExpiry = now.Add(_options.AbsoluteSessionLifetime);

        var session = new UserSession(
            user.Id,
            now,
            sessionExpiry,
            absoluteSessionExpiry,
            requestContext.IpAddress,
            requestContext.UserAgent);

        user.RecordLogin(now);
        context.UserSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);

        return new DashboardLoginResponse(user.Id, user.Role, session.Id);
    }
}
