using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Infrastructure.Database;
using BillingFlow.Infrastructure.Outbox;
using BillingFlow.Infrastructure.Outbox.Models;

using Dapper;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Infrastructure.BackgroundJobs.Recurring;

/// <summary>
/// The "Relay" component of the Transactional Outbox Pattern.
/// Periodically sweeps the database for pending integration messages and dispatches them via the configured transport.
/// </summary>
public class ProcessOutboxMessagesJob(
    BillingDbContext context,
    IDbConnectionFactory connectionFactory,
    IIntegrationEventDispatcher dispatcher,
    TimeProvider timeProvider,
    ILogger<ProcessOutboxMessagesJob> logger)
{
    private const int MaxAttempts = 5;

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var lockExpiration = now.AddMinutes(2);

        // 1. ATOMIC CLAIM (Concurrency Control)
        // Utilizes raw SQL Server capabilities to safely reserve a batch of messages.
        // UPDLOCK: Acquires an update lock on the rows.
        // READPAST: Skips rows locked by other worker instances, eliminating deadlocks during horizontal scaling.
        var claimSql = @"
            WITH PendingMessages AS (
                SELECT TOP (50) *
                FROM OutboxMessages WITH (UPDLOCK, READPAST)
                WHERE Status = 0 /* Pending */
                  AND (LockedUntil IS NULL OR LockedUntil < @Now)
                  AND (NextAttemptAt IS NULL OR NextAttemptAt <= @Now)
                ORDER BY OccurredOn ASC
            )
            UPDATE PendingMessages
            SET LockedUntil = @LockedUntil,
                AttemptCount = AttemptCount + 1
            OUTPUT inserted.*;";

        using var connection = connectionFactory.CreateConnection();

        var messages = await connection.QueryAsync<OutboxMessage>(
            claimSql,
            new { LockedUntil = lockExpiration, Now = now });

        if (!messages.Any()) return;

        // 2. DISPATCH BATCH
        foreach (var message in messages)
        {
            try
            {
                // Passing message.Id for At-Least-Once deduplication downstream
                await dispatcher.DispatchAsync(message.Id, message.Type, message.Payload, cancellationToken);
                message.MarkAsProcessed(timeProvider.GetUtcNow());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to dispatch outbox message {Id}. Attempt {Count}/{Max}",
                    message.Id, message.AttemptCount, MaxAttempts);

                message.MarkAsFailed(ex.Message, timeProvider.GetUtcNow(), MaxAttempts);
            }
        }

        // 3. COMMIT STATUS UPDATES
        // Write the new states (Processed/Failed) back to the database.
        context.OutboxMessages.UpdateRange(messages);
        await context.SaveChangesAsync(cancellationToken);
    }
}
