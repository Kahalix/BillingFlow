// File: src/BillingFlow.Application/Features/Identity/Commands/ChangePassword/ChangePasswordHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.ChangePassword;

public class ChangePasswordHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;

        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException("User context is invalid.");
        }

        // Verify the current password matches the one in the database
        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException("The current password provided is incorrect.");
        }

        // Hash the new password and update the domain entity
        var newPasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.ChangePassword(newPasswordHash);

        // Save changes (this will also trigger the DispatchDomainEventsInterceptor 
        // to publish PasswordChangedEvent)
        await context.SaveChangesAsync(cancellationToken);
    }
}
