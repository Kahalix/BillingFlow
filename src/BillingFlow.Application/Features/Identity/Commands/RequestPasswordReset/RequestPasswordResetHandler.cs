using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Features.Identity.IntegrationEvents;
using BillingFlow.Application.Identity;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillingFlow.Application.Features.Identity.Commands.RequestPasswordReset;

/// <summary>
/// Handles the initiation of a password reset flow.
/// Orchestrates token generation and durably queues the email transmission via the Transactional Outbox.
/// </summary>
public class RequestPasswordResetHandler(
    IApplicationDbContext context,
    ITokenGenerator tokenGenerator,
    ITokenHashService tokenHashService,
    IIntegrationEventPublisher eventPublisher,
    IOptions<IdentityOptions> options,
    TimeProvider timeProvider)
    : ICommandHandler<RequestPasswordResetCommand>
{
    private readonly IdentityOptions _options = options.Value;

    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Security: Do not reveal if the user exists. Just return silently.
        // We also don't allow password resets for suspended/inactive accounts.
        if (user is null || user.Status != UserStatus.Active)
        {
            return Result.Success();
        }

        // 1. Generate a secure URL-safe token (Not a JWT)
        var rawToken = tokenGenerator.GenerateSecureToken();
        var tokenHash = tokenHashService.HashToken(rawToken);

        var now = timeProvider.GetUtcNow();
        var expiry = now.Add(_options.PasswordResetLifetime);

        // 2. Use the dedicated factory method to create the token
        var userToken = UserToken.CreatePasswordResetToken(
            user.Id,
            tokenHash,
            now,
            expiry);

        context.UserTokens.Add(userToken);

        // We declare the intent to send the email BEFORE committing the transaction.
        // The publisher serializes this intent into the EF Core ChangeTracker.
        // Both the UserToken and the OutboxMessage will be saved atomically in Step 4.
        eventPublisher.Publish(new SendPasswordResetEmailEvent(user.Email, rawToken));

        // 4. Commit everything atomically
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
