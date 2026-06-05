// File: src/BillingFlow.Application/Features/Payments/Commands/CreatePaymentSession/CreatePaymentSessionRequest.cs
using System;

namespace BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;

public record CreatePaymentSessionRequest(
    Guid InvoiceId,
    string SuccessUrl,
    string CancelUrl
);
