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

namespace BillingFlow.Application.Features.Identity.Commands.InitiateUserEmailChange;

public class InitiateUserEmailChangeHandler(
    IApplicationDbContext context,
    ITokenGenerator tokenGenerator,
    ITokenHashService tokenHashService,
    IIntegrationEventPublisher eventPublisher,
    IOptions<IdentityOptions> options,
    TimeProvider timeProvider)
    : IRequestHandler<InitiateUserEmailChangeCommand>
{
    private readonly IdentityOptions _options = options.Value;
    public async Task Handle(InitiateUserEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var normalizedNewEmail = request.NewEmail.Trim().ToLowerInvariant();

        // 1. Ensure the target user actually exists
        var targetUserExists = await context.Users.AnyAsync(u => u.Id == request.TargetUserId, cancellationToken);
        if (!targetUserExists)
        {
            throw new NotFoundException(nameof(AppUser), request.TargetUserId);
        }

        // 2. Check if the new email is already in use by another user
        var emailExists = await context.Users.AnyAsync(u => u.Email == normalizedNewEmail, cancellationToken);
        if (emailExists)
        {
            throw new DomainException("This email address is already associated with another account.");
        }

        // 3. Generate token and save
        var rawToken = tokenGenerator.GenerateSecureToken();
        var tokenHash = tokenHashService.HashToken(rawToken);

        var now = timeProvider.GetUtcNow();
        var expiry = now.Add(_options.EmailConfirmationLifetime);

        // Use the dedicated factory method to create the email confirmation token
        var token = UserToken.CreateEmailConfirmationToken(
            request.TargetUserId,
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
