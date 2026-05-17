// File: src/BillingFlow.Application/Features/Identity/Commands/InitiateMyEmailChange/InitiateMyEmailChangeHandler.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;
using BillingFlow.Application.Common.Exceptions;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateMyEmailChange;

public class InitiateMyEmailChangeHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator,
    ITokenHashService tokenHashService,
    IBackgroundJobClient backgroundJobs,
    TimeProvider timeProvider)
    : IRequestHandler<InitiateMyEmailChangeCommand>
{
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
        var expiry = timeProvider.GetUtcNow().AddHours(24);

        var token = new UserToken(
            currentUserId,
            Guid.Empty,
            UserTokenType.EmailConfirmation,
            tokenHash,
            expiry,
            data: normalizedNewEmail);

        context.UserTokens.Add(token);
        await context.SaveChangesAsync(cancellationToken);

        // 4. Dispatch the email via Hangfire
        backgroundJobs.Enqueue<IEmailSender>(sender =>
            sender.SendEmailChangeConfirmationAsync(normalizedNewEmail, rawToken, CancellationToken.None));
    }
}
