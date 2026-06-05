using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.ResetPassword;

public class ResetPasswordHandler(
    IApplicationDbContext context,
    ITokenHashService tokenHashService,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
    : IRequestHandler<ResetPasswordCommand>
{
    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var incomingTokenHash = tokenHashService.HashToken(request.Token);

        // 1. Fetch user by email
        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || user.Status == UserStatus.Suspended)
        {
            ThrowInvalidTokenException();
        }

        // 2. Find the active reset token for this user
        var resetToken = await context.UserTokens
            .Where(t => t.UserId == user!.Id && t.Type == UserTokenType.PasswordReset)
            .SingleOrDefaultAsync(t => t.TokenHash == incomingTokenHash, cancellationToken);

        // 3. Verify token validity
        if (resetToken is null || !resetToken.IsActive(timeProvider))
        {
            ThrowInvalidTokenException();
        }

        // 4. Update password securely
        var newPasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user!.ChangePassword(newPasswordHash);

        // 5. Invalidate the token
        resetToken!.MarkAsConsumed(timeProvider);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ThrowInvalidTokenException()
    {
        throw new DomainException("The password reset token is invalid or has expired.");
    }
}
