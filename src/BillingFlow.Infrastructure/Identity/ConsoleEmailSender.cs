// File: src/BillingFlow.Infrastructure/Identity/ConsoleEmailSender.cs
using BillingFlow.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// A mock implementation of IEmailSender for local development.
/// Outputs email contents directly to the application console/logs.
/// </summary>
public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("========== MOCK EMAIL SENDER ==========");
        logger.LogInformation("To: {Email}", toEmail);
        logger.LogInformation("Subject: Password Reset Request");
        logger.LogInformation("Body: You requested a password reset. Please use the following token: {Token}", resetToken);
        logger.LogInformation("=======================================");

        return Task.CompletedTask;
    }

    public Task SendEmailChangeConfirmationAsync(string newEmail, string confirmationToken, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("========== MOCK EMAIL SENDER ==========");
        logger.LogInformation("To: {Email}", newEmail);
        logger.LogInformation("Subject: Email Address Change Confirmation");
        logger.LogInformation("Body: Please use the following token to confirm your new email address: {Token}", confirmationToken);
        logger.LogInformation("=======================================");

        return Task.CompletedTask;
    }

    public Task SendEmailChangedNoticeAsync(string oldEmail, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("========== MOCK EMAIL SENDER ==========");
        logger.LogInformation("To: {Email}", oldEmail);
        logger.LogInformation("Subject: SECURITY ALERT - Email Address Changed");
        logger.LogInformation("Body: The email address associated with your account was just changed. If you did not authorize this, contact support immediately!");
        logger.LogInformation("=======================================");

        return Task.CompletedTask;
    }
}
