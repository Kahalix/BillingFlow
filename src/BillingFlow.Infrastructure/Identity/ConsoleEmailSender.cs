using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Infrastructure.Database;
using BillingFlow.Infrastructure.Outbox.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Implementation of IEmailSender using an Idempotent Receiver pattern.
/// Provides best-effort deduplication for At-Least-Once delivery scenarios using Lease Tokens.
/// </summary>
public class ConsoleEmailSender(
    BillingDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    private async Task ExecuteIdempotentlyAsync(Guid outboxMessageId, string handlerName, Func<Task> emailAction, CancellationToken cancellationToken)
    {
        bool leaseAcquired = false;
        var now = timeProvider.GetUtcNow();

        // Zombie Worker Protection: Unique token identifying THIS specific worker's execution attempt
        var currentLeaseToken = Guid.NewGuid();
        var leaseExpiresAt = now.AddMinutes(2);

        // 1. ATTEMPT INITIAL INSERT (Fast path)
        try
        {
            dbContext.IntegrationDispatchLogs.Add(
                new IntegrationDispatchLog(outboxMessageId, handlerName, DispatchStatus.Processing, now, currentLeaseToken, leaseExpiresAt));

            // Safely routes through BillingDbContext translator, throwing UniqueConstraintException on duplicates
            await dbContext.SaveChangesAsync(cancellationToken);
            leaseAcquired = true;
        }
        catch (UniqueConstraintException)
        {
            // 2. ATTEMPT TO RECOVER ABANDONED LEASE (Slow path)
            int rowsAffected = await dbContext.IntegrationDispatchLogs
                .Where(log =>
                    log.OutboxMessageId == outboxMessageId &&
                    log.HandlerName == handlerName &&
                    (
                        log.Status == DispatchStatus.Failed ||
                        (log.Status == DispatchStatus.Processing && log.LeaseExpiresAt < now)
                    ))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, DispatchStatus.Processing)
                    .SetProperty(b => b.UpdatedAt, now)
                    .SetProperty(b => b.LeaseToken, currentLeaseToken)
                    .SetProperty(b => b.LeaseExpiresAt, leaseExpiresAt),
                    cancellationToken);

            if (rowsAffected > 0)
            {
                leaseAcquired = true;
            }
        }

        // 3. CHECK AUTHORIZATION
        if (!leaseAcquired)
        {
            logger.LogInformation("Idempotency check: Message {Id} for Handler '{HandlerName}' is already completed or actively processing. Skipping.", outboxMessageId, handlerName);
            return;
        }

        logger.LogInformation("Idempotency lease acquired for message {Id} (Handler: {HandlerName}) with token {Token}.", outboxMessageId, handlerName, currentLeaseToken);

        // 4. EXECUTE THE SIDE-EFFECT
        try
        {
            await emailAction();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "External network dispatch failed for message {Id} (Handler: {HandlerName}). Releasing lease as Failed.", outboxMessageId, handlerName);

            // 5a. MARK AS FAILED (Zombie Worker Protection: Must match currentLeaseToken and state)
            await dbContext.IntegrationDispatchLogs
                .Where(log =>
                    log.OutboxMessageId == outboxMessageId &&
                    log.HandlerName == handlerName &&
                    log.LeaseToken == currentLeaseToken &&
                    log.Status == DispatchStatus.Processing)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(b => b.Status, DispatchStatus.Failed)
                    .SetProperty(b => b.UpdatedAt, timeProvider.GetUtcNow()),
                    cancellationToken);

            throw;
        }

        // 5b. MARK AS COMPLETED (Zombie Worker Protection: Must match currentLeaseToken and state)
        await dbContext.IntegrationDispatchLogs
            .Where(log =>
                log.OutboxMessageId == outboxMessageId &&
                log.HandlerName == handlerName &&
                log.LeaseToken == currentLeaseToken &&
                log.Status == DispatchStatus.Processing)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, DispatchStatus.Completed)
                .SetProperty(b => b.UpdatedAt, timeProvider.GetUtcNow()),
                cancellationToken);

        logger.LogInformation("Idempotency cycle successfully completed for message {Id} (Handler: {HandlerName}).", outboxMessageId, handlerName);
    }

    public Task SendPasswordResetEmailAsync(Guid outboxMessageId, string toEmail, string resetToken, CancellationToken cancellationToken = default)
    {
        return ExecuteIdempotentlyAsync(outboxMessageId, nameof(SendPasswordResetEmailAsync), () =>
        {
            logger.LogInformation("To: {Email} | Action: Password Reset", toEmail);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SendEmailChangeConfirmationAsync(Guid outboxMessageId, string newEmail, string confirmationToken, CancellationToken cancellationToken = default)
    {
        return ExecuteIdempotentlyAsync(outboxMessageId, nameof(SendEmailChangeConfirmationAsync), () =>
        {
            logger.LogInformation("To: {Email} | Action: Email Change Confirmation", newEmail);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SendEmailChangedNoticeAsync(Guid outboxMessageId, string oldEmail, CancellationToken cancellationToken = default)
    {
        return ExecuteIdempotentlyAsync(outboxMessageId, nameof(SendEmailChangedNoticeAsync), () =>
        {
            logger.LogInformation("To: {Email} | Action: Security Notice Email Changed", oldEmail);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SendClientSuspensionNoticeAsync(Guid outboxMessageId, string toEmail, string companyName, CancellationToken cancellationToken = default)
    {
        return ExecuteIdempotentlyAsync(outboxMessageId, nameof(SendClientSuspensionNoticeAsync), () =>
        {
            logger.LogInformation("To: {Email} | Action: Suspension Notice for {Company}", toEmail, companyName);
            return Task.CompletedTask;
        }, cancellationToken);
    }

    public Task SendInvoiceOverdueNoticeAsync(Guid outboxMessageId, string toEmail, string companyName, string invoiceNumber, decimal amountDue, CancellationToken cancellationToken = default)
    {
        return ExecuteIdempotentlyAsync(outboxMessageId, nameof(SendInvoiceOverdueNoticeAsync), () =>
        {
            logger.LogInformation("To: {Email} | Action: Overdue Invoice {No} for Amount {Amt}", toEmail, invoiceNumber, amountDue);
            return Task.CompletedTask;
        }, cancellationToken);
    }
}
