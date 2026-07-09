using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Identity.IntegrationEvents;
using BillingFlow.Application.Identity;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using MediatR;

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
    : IRequestHandler<InitiateMyEmailChangeCommand>
{
    private readonly IdentityOptions _options = options.Value;
    public async Task Handle(InitiateMyEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var normalizedNewEmail = request.NewEmail.Trim().ToLowerInvariant();

        // 1. Fetch current user and verify step-up security (password)
        var user = await context.Users.SingleOrDefaultAsync(u => u.Id == currentUserId, cancellationToken)
            ?? throw new UnauthorizedException("User context is invalid.");

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new DomainException("Invalid current password.");
        }

        // 2. Check if the new email is already in use
        var emailExists = await context.Users.AnyAsync(u => u.Email == normalizedNewEmail, cancellationToken);
        if (emailExists)
        {
            throw new DomainException("This email address is already in use.");
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
    }
}
