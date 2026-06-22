namespace BillingFlow.Infrastructure.Outbox.Models;

/// <summary>
/// Represents the lifecycle state of a technical integration message in the outbox.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>Message is waiting to be picked up by the background relay worker.</summary>
    Pending = 0,

    /// <summary>Message was successfully dispatched to the external broker.</summary>
    Processed = 1,

    /// <summary>Message failed consistently and was moved to the Dead-Letter Queue (DLQ).</summary>
    Failed = 2
}
