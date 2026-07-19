using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Features.Identity.Commands.ResetPassword;

public class ResetPasswordHandler(
    IApplicationDbContext context,
    ITokenHashService tokenHashService,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var incomingTokenHash = tokenHashService.HashToken(request.Token);

        // 1. Fetch user by email
        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || user.Status == UserStatus.Suspended)
        {
            return Result.Failure(Errors.Tokens.InvalidOrExpired);
        }

        // 2. Find the active reset token for this user
        var resetToken = await context.UserTokens
            .Where(t => t.UserId == user!.Id && t.Type == UserTokenType.PasswordReset)
            .SingleOrDefaultAsync(t => t.TokenHash == incomingTokenHash, cancellationToken);

        var now = timeProvider.GetUtcNow();

        // 3. Verify token validity
        if (resetToken is null || !resetToken.IsActive(now))
        {
            return Result.Failure(Errors.Tokens.InvalidOrExpired);
        }

        // 4. Update password securely
        var newPasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user!.ChangePassword(newPasswordHash);

        // 5. Invalidate the token
        resetToken!.MarkAsConsumed(now);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
