// File: src/BillingFlow.Domain/Events/InvoiceCanceledEvent.cs
using System;

using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

public record InvoiceCanceledEvent(
    Guid InvoiceId,
    Guid ClientId,
    decimal TotalAmount) : IDomainEvent;
