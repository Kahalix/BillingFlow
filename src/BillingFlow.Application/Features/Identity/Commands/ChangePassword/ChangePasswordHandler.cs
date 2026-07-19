using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Features.Identity.Commands.ChangePassword;

public class ChangePasswordHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;

        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(Errors.Auth.InvalidSession);
        }

        // Verify the current password matches the one in the database
        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(Errors.Users.InvalidPassword);
        }

        // Hash the new password and update the domain entity
        var newPasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.ChangePassword(newPasswordHash);

        // Save changes (this will also trigger the DispatchDomainEventsInterceptor 
        // to publish PasswordChangedEvent)
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
