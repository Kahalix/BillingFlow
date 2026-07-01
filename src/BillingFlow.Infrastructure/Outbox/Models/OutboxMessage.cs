using System;

namespace BillingFlow.Infrastructure.Outbox.Models;

/// <summary>
/// Technical integration log strictly owned by the Infrastructure layer.
/// Represents the persistent state of an outbound message, including concurrency locks and retry mechanics.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; }
    public string Payload { get; private set; }
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }

    // Concurrency & Resilience management fields
    public OutboxMessageStatus Status { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    /// <summary>Required by EF Core.</summary>
    protected OutboxMessage()
    {
        Type = string.Empty;
        Payload = string.Empty;
    }

    public OutboxMessage(string type, string payload, DateTimeOffset occurredOn)
    {
        Id = Guid.NewGuid();
        Type = type;
        Payload = payload;
        OccurredOn = occurredOn;
        Status = OutboxMessageStatus.Pending;
        AttemptCount = 0;
    }

    /// <summary>
    /// Flags the message as successfully dispatched, freeing locks and clearing errors.
    /// </summary>
    public void MarkAsProcessed(DateTimeOffset processedOn)
    {
        Status = OutboxMessageStatus.Processed;
        ProcessedOn = processedOn;
        LockedUntil = null;
        LastError = null;
    }

    /// <summary>
    /// Records a dispatch failure. Implements an exponential backoff strategy for subsequent retries,
    /// ultimately transitioning the message to a Failed (Dead-Letter) state if max attempts are exceeded.
    /// </summary>
    public void MarkAsFailed(string error, DateTimeOffset now, int maxAttempts)
    {
        LastError = error;
        LockedUntil = null; // Release the concurrency lock immediately

        if (AttemptCount >= maxAttempts)
        {
            Status = OutboxMessageStatus.Failed;
            NextAttemptAt = null; // Permanent failure
        }
        else
        {
            // Exponential backoff (2^0 = 1m, 2^1 = 2m, 2^2 = 4m, 2^3 = 8m, etc.)
            NextAttemptAt = now.AddMinutes(Math.Pow(2, AttemptCount - 1));
        }
    }
}
