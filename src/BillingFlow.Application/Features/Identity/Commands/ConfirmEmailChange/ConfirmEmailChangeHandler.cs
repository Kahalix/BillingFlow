// File: src/BillingFlow.Application/Features/Identity/Commands/ConfirmEmailChange/ConfirmEmailChangeHandler.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.ConfirmEmailChange;

public class ConfirmEmailChangeHandler(
    IApplicationDbContext context,
    ITokenHashService tokenHashService,
    TimeProvider timeProvider)
    : IRequestHandler<ConfirmEmailChangeCommand>
{
    public async Task Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var incomingTokenHash = tokenHashService.HashToken(request.Token);

        // 1. Find the active token of type EmailConfirmation
        var resetToken = await context.UserTokens
            .SingleOrDefaultAsync(t => t.TokenHash == incomingTokenHash && t.Type == UserTokenType.EmailConfirmation, cancellationToken);

        if (resetToken is null || !resetToken.IsActive(timeProvider) || string.IsNullOrWhiteSpace(resetToken.Data))
        {
            throw new DomainException("The email confirmation token is invalid or has expired.");
        }

        // 2. Fetch the associated user
        var user = await context.Users.SingleOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken)
            ?? throw new DomainException("User not found.");

        var newEmail = resetToken.Data;

        // 3. Additional security check: ensure the email hasn't been claimed by another user in the meantime
        var emailAlreadyTaken = await context.Users.AnyAsync(u => u.Email == newEmail && u.Id != user.Id, cancellationToken);
        if (emailAlreadyTaken)
        {
            throw new DomainException("This email address was registered by another user in the meantime.");
        }

        // 4. Change the email (This will dispatch the EmailChangedEvent!)
        user.ChangeEmail(newEmail);
        resetToken.MarkAsConsumed(timeProvider);

        await context.SaveChangesAsync(cancellationToken);
    }
}
