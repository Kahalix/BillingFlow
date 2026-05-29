// File: src/BillingFlow.Domain/Events/InvoicePaidEvent.cs
using System;

using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when an invoice is fully paid.
/// Carries financial state to efficiently update Read Models without DB roundtrips.
/// </summary>
public record InvoicePaidEvent(
    Guid InvoiceId,
    Guid ClientId,
    decimal TotalInvoiceAmount) : IDomainEvent;
