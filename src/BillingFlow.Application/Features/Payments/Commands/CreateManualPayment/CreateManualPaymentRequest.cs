using System;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Payments.Commands.CreateManualPayment;

/// <summary>
/// HTTP payload schema for registering a manual back-office payment.
/// </summary>
public record CreateManualPaymentRequest(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    DateTimeOffset PaymentDate,
    string? Notes
);
