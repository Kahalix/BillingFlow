// File: src/BillingFlow.Application/Features/Identity/Commands/RequestPasswordReset/RequestPasswordResetHandler.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.RequestPasswordReset;

public class RequestPasswordResetHandler(
    IApplicationDbContext context,
    ITokenGenerator tokenGenerator,
    ITokenHashService tokenHashService,
    IBackgroundJobClient backgroundJobs,
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
        var expiry = timeProvider.GetUtcNow().AddHours(1);

        // Note: Password reset tokens don't need a SessionId, so we can use Guid.Empty
        var userToken = new UserToken(
            user.Id,
            Guid.Empty,
            UserTokenType.PasswordReset,
            tokenHash,
            expiry);

        context.UserTokens.Add(userToken);
        await context.SaveChangesAsync(cancellationToken);

        // 3. Dispatch the email via Hangfire (Background Job)
        // This prevents the HTTP request from blocking/hanging while waiting for the SMTP server response.
        backgroundJobs.Enqueue<IEmailSender>(sender =>
            sender.SendPasswordResetEmailAsync(user.Email, rawToken, CancellationToken.None));
    }
}
