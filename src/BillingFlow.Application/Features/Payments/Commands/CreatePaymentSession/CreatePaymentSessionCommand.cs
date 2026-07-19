using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;

/// <summary>
/// Triggers the creation of a Stripe Checkout Session for a given invoice.
/// </summary>
public record CreatePaymentSessionCommand(
    Guid InvoiceId,
    string SuccessUrl,
    string CancelUrl
) : ICommand<PaymentSessionResponse>, IRequirePolicy;
