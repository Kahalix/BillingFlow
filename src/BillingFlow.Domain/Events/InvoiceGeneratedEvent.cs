using System;

using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when an invoice is formally issued (transitions from Draft to Unpaid).
/// Used to trigger asynchronous updates to the Client Balance read models.
/// </summary>
public record InvoiceGeneratedEvent(Guid InvoiceId, Guid ClientId, decimal TotalAmount) : IDomainEvent;
