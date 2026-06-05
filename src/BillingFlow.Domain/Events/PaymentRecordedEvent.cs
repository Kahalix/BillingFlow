using System;

using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// A strict financial event indicating that cash has successfully entered the system.
/// This acts as the Source of Truth for projecting client balances.
/// </summary>
public record PaymentRecordedEvent(
    Guid PaymentId,
    Guid InvoiceId,
    Guid ClientId,
    decimal Amount) : IDomainEvent;
