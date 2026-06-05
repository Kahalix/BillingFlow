using System;

namespace BillingFlow.Application.Features.Payments.Commands.CreateManualPayment;

/// <summary>
/// HTTP response contract for successfully created payment ledgers.
/// </summary>
public record PaymentCreatedResponse(Guid Id);
