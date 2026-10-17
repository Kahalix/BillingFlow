using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Payments.Commands.ProcessStripeWebhook;

/// <summary>
/// Encapsulates the raw HTTP payload and signature from a Stripe webhook request.
/// </summary>
public record ProcessStripeWebhookCommand(string JsonPayload, string StripeSignature) : ICommand, IPublicRequest;
