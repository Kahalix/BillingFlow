
using System;

namespace BillingFlow.Infrastructure.Outbox.Models;

/// <summary>
/// Ensures downstream best-effort deduplication for At-Least-Once delivery.
/// Acts as a distributed state machine checkpoint to prevent concurrent race conditions.
/// </summary>
public class IntegrationDispatchLog
{
    public Guid Id { get; private set; }
    public Guid OutboxMessageId { get; private set; }

    public string HandlerName { get; private set; }

    public DispatchStatus Status { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Zombie Worker Protection (Lease / Ownership)
    public Guid LeaseToken { get; private set; }
    public DateTimeOffset LeaseExpiresAt { get; private set; }

    protected IntegrationDispatchLog()
    {
        HandlerName = string.Empty;
    }

    public IntegrationDispatchLog(
        Guid outboxMessageId,
        string handlerName,
        DispatchStatus status,
        DateTimeOffset updatedAt,
        Guid leaseToken,
        DateTimeOffset leaseExpiresAt)
    {
        Id = Guid.NewGuid();
        OutboxMessageId = outboxMessageId;
        HandlerName = handlerName;
        Status = status;
        UpdatedAt = updatedAt;
        LeaseToken = leaseToken;
        LeaseExpiresAt = leaseExpiresAt;
    }
}
