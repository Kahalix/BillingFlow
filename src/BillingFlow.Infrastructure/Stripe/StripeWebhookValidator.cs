using System;

using Stripe;
using Stripe.Checkout;

using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Domain.Enums;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Infrastructure.Stripe;

public class StripeWebhookValidator(
    IConfiguration configuration,
    ILogger<StripeWebhookValidator> logger) : IStripeWebhookValidator
{
    private readonly string _webhookSecret = configuration["Stripe:WebhookSecret"]
        ?? throw new InvalidOperationException("Stripe Webhook Secret is missing from configuration.");

    public PaymentCompletedEventDto? ValidateAndParse(string jsonPayload, string signatureHeader)
    {
        try
        {
            // Cryptographically verifies the payload using the webhook secret.
            // If the signature doesn't match the payload, this method throws a StripeException.
            var stripeEvent = EventUtility.ConstructEvent(jsonPayload, signatureHeader, _webhookSecret);

            // Use the raw Stripe API string to prevent namespace collisions.
            // This avoids conflicts between the Stripe.net 'Events' class and our domain 'Events' namespace.
            if (stripeEvent.Type == "checkout.session.completed")
            {
                if (stripeEvent.Data.Object is Session session)
                {
                    if (session.Metadata.TryGetValue("InvoiceId", out var invoiceIdStr) &&
                        Guid.TryParse(invoiceIdStr, out var invoiceId))
                    {
                        // The session only knows allowed methods, not the actual method used (which sits in PaymentIntent).
                        // To avoid false accounting data, we explicitly mark it as Unknown.
                        return new PaymentCompletedEventDto(stripeEvent.Id, session.Id, invoiceId, Domain.Enums.PaymentMethod.Unknown);
                    }

                    logger.LogWarning("Received CheckoutSessionCompleted without a valid InvoiceId in metadata. Session ID: {SessionId}", session.Id);
                }
            }

            // Return null for unhandled but valid events to safely acknowledge them (HTTP 200 OK)
            return null;
        }
        catch (StripeException ex)
        {
            // The signature is invalid, or the payload was tampered with (Spoofing attempt).
            // We throw a domain-specific exception rather than a generic infrastructure one.
            logger.LogWarning(ex, "Failed to validate Stripe webhook signature. Possible spoofing attempt.");
            throw new InvalidWebhookSignatureException("Invalid webhook signature.", ex);
        }
    }
}
