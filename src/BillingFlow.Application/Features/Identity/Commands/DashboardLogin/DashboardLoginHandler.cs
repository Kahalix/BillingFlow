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

namespace BillingFlow.Application.Features.Identity.Commands.DashboardLogin;

public class DashboardLoginHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IRequestContext requestContext,
    IOptions<IdentityOptions> options,
    TimeProvider timeProvider) : ICommandHandler<DashboardLoginCommand, DashboardLoginResponse>
{
    private readonly IdentityOptions _options = options.Value;

    public async Task<Result<DashboardLoginResponse>> Handle(DashboardLoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<DashboardLoginResponse>(Errors.Auth.InvalidCredentials);
        }

        if (user.Status != UserStatus.Active)
        {
            return Result.Failure<DashboardLoginResponse>(Errors.Users.Suspended);
        }

        if (user.Role != Role.Admin)
        {
            return Result.Failure<DashboardLoginResponse>(Errors.Users.AdminRequired);
        }

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

        return Result.Success(new DashboardLoginResponse(user.Id, user.Role, session.Id));
    }
}
