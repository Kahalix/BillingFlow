using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Features.Identity.IntegrationEvents;
using BillingFlow.Application.Identity;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateMyEmailChange;

public class InitiateMyEmailChangeHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    ITokenHashService tokenHashService,
    IIntegrationEventPublisher eventPublisher,
    IOptions<IdentityOptions> options,
    TimeProvider timeProvider)
    : ICommandHandler<InitiateMyEmailChangeCommand>
{
    private readonly IdentityOptions _options = options.Value;

    public async Task<Result> Handle(InitiateMyEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var normalizedNewEmail = request.NewEmail.Trim().ToLowerInvariant();

        // 1. Fetch current user and verify step-up security (password)
        var user = await context.Users.SingleOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Errors.Auth.InvalidSession);
        }

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(Errors.Users.InvalidPassword);
        }

        // 2. Check if the new email is already in use
        var emailExists = await context.Users.AnyAsync(u => u.Email == normalizedNewEmail, cancellationToken);
        if (emailExists)
        {
            return Result.Failure(Errors.Users.EmailInUse);
        }

        // 3. Generate token and save
        var rawToken = tokenGenerator.GenerateSecureToken();
        var tokenHash = tokenHashService.HashToken(rawToken);

        var now = timeProvider.GetUtcNow();
        var expiry = now.Add(_options.EmailConfirmationLifetime);

        // Use the dedicated factory method to create the email confirmation token
        var token = UserToken.CreateEmailConfirmationToken(
            currentUserId,
            tokenHash,
            normalizedNewEmail, // Payload Data
            now,
            expiry);

        context.UserTokens.Add(token);

        // 4. Register the outbound email intent in the same atomic transaction
        eventPublisher.Publish(new SendEmailChangeConfirmationEvent(normalizedNewEmail, rawToken));

        // 5. Commit state and integration log atomically
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
