// File: src/BillingFlow.Application/Features/Payments/Queries/GetPayments/PaymentSummaryDto.cs
using System;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Payments.Queries.GetPayments;

/// <summary>
/// A lightweight read model representing a payment record in a list/table view.
/// </summary>
public record PaymentSummaryDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    DateTimeOffset PaymentDate,
    PaymentProvider Provider,
    PaymentMethod Method,
    string? ExternalTransactionId
);
