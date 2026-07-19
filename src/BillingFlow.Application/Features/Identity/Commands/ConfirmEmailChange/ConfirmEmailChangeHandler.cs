using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Features.Identity.Commands.ConfirmEmailChange;

public class ConfirmEmailChangeHandler(
    IApplicationDbContext context,
    ITokenHashService tokenHashService,
    TimeProvider timeProvider)
    : ICommandHandler<ConfirmEmailChangeCommand>
{
    public async Task<Result> Handle(ConfirmEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var incomingTokenHash = tokenHashService.HashToken(request.Token);

        // 1. Find the active token of type EmailConfirmation
        var resetToken = await context.UserTokens
            .SingleOrDefaultAsync(t => t.TokenHash == incomingTokenHash && t.Type == UserTokenType.EmailConfirmation, cancellationToken);

        var now = timeProvider.GetUtcNow();

        if (resetToken is null || !resetToken.IsActive(now) || string.IsNullOrWhiteSpace(resetToken.Data))
        {
            return Result.Failure(Errors.Tokens.InvalidOrExpired);
        }

        // 2. Fetch the associated user
        var user = await context.Users.SingleOrDefaultAsync(u => u.Id == resetToken.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Errors.Users.NotFound(resetToken.UserId));
        }

        var newEmail = resetToken.Data;

        // 3. Additional security check: ensure the email hasn't been claimed by another user in the meantime
        var emailAlreadyTaken = await context.Users.AnyAsync(u => u.Email == newEmail && u.Id != user.Id, cancellationToken);
        if (emailAlreadyTaken)
        {
            return Result.Failure(Errors.Tokens.ClaimedByOtherUser);
        }

        // 4. Change the email (This will dispatch the EmailChangedEvent!)
        user.ChangeEmail(newEmail);

        // Używamy wyliczonego 'now'
        resetToken.MarkAsConsumed(now);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
