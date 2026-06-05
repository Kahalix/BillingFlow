// File: src/BillingFlow.Application/Features/Payments/Queries/GetPaymentDetails/PaymentDetailsResponse.cs
using System;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Payments.Queries.GetPaymentDetails;

public record PaymentDetailsResponse(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    DateTimeOffset PaymentDate,
    PaymentProvider Provider,
    PaymentMethod Method,
    string? ExternalTransactionId,
    Guid? ReceivedByUserId,
    string? Notes,
    DateTimeOffset CreatedAt
);
