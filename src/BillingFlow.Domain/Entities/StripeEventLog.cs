// File: src/BillingFlow.Domain/Entities/StripeEventLog.cs
using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// Represents an immutable log record of an incoming Stripe webhook event.
/// Used to guarantee strict idempotent processing at the core database level.
/// </summary>
public class StripeEventLog : Entity, IAggregateRoot
{
    /// <summary>
    /// The unique identifier assigned by Stripe (e.g., evt_1N2345...).
    /// This field is protected by a database-level UNIQUE constraint.
    /// </summary>
    public string EventId { get; private set; } = null!;

    /// <summary>
    /// The exact timestamp when this event was processed and committed to our ledger.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; private set; }

    /// <summary>
    /// Required by Entity Framework Core for materialization.
    /// </summary>
    protected StripeEventLog() { }

    /// <summary>
    /// Initializes a new instance of the StripeEventLog aggregate.
    /// </summary>
    /// <param name="eventId">The unique, raw Stripe event ID string.</param>
    /// <param name="now">The standardized transaction timestamp.</param>
    public StripeEventLog(string eventId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new DomainException("The Stripe Event ID cannot be null or empty.");

        Id = Guid.NewGuid();
        EventId = eventId.Trim();
        ProcessedAt = now;
    }
}
