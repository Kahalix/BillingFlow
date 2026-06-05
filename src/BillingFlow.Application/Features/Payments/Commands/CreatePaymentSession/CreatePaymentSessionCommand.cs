using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;

/// <summary>
/// Triggers the creation of a Stripe Checkout Session for a given invoice.
/// </summary>
public record CreatePaymentSessionCommand(
    Guid InvoiceId,
    string SuccessUrl,
    string CancelUrl
) : IRequest<PaymentSessionResponse>, IRequirePolicy;
