using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Identity.IntegrationEvents;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

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
    TimeProvider timeProvider)
    : IRequestHandler<RequestPasswordResetCommand>
{
    public async Task Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Security: Do not reveal if the user exists. Just return silently.
        // We also don't allow password resets for suspended/inactive accounts.
        if (user is null || user.Status != UserStatus.Active)
        {
            return;
        }

        // 1. Generate a secure URL-safe token (Not a JWT)
        var rawToken = tokenGenerator.GenerateSecureToken();
        var tokenHash = tokenHashService.HashToken(rawToken);

        // 2. Create the domain entity for the token (Valid for 1 hour)
        var now = timeProvider.GetUtcNow();
        var expiry = now.AddHours(1);

        // Note: Password reset tokens don't need a SessionId, so we can use Guid.Empty
        var userToken = new UserToken(
            user.Id,
            Guid.Empty,
            UserTokenType.PasswordReset,
            tokenHash,
            expiry,
            now);

        context.UserTokens.Add(userToken);

        // We declare the intent to send the email BEFORE committing the transaction.
        // The publisher serializes this intent into the EF Core ChangeTracker.
        // Both the UserToken and the OutboxMessage will be saved atomically in Step 4.
        eventPublisher.Publish(new SendPasswordResetEmailEvent(user.Email, rawToken));

        // 4. Commit everything atomically
        await context.SaveChangesAsync(cancellationToken);
    }
}
