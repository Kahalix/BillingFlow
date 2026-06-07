using System;
using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Triggered when an invoice surpasses its due date without being fully paid.
/// Used to initiate debt-collection workflows, such as sending overdue email notices.
/// </summary>
public record InvoiceOverdueEvent(
    Guid InvoiceId,
    Guid ClientId,
    decimal AmountDue) : IDomainEvent;
