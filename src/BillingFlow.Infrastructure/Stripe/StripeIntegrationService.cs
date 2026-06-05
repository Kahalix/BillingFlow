// File: src/BillingFlow.Infrastructure/Stripe/StripeIntegrationService.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using Stripe;
using Stripe.Checkout;

namespace BillingFlow.Infrastructure.Stripe;

/// <summary>
/// Encapsulates the Stripe API communication.
/// Operates as a stateless infrastructure service.
/// </summary>
public class StripeIntegrationService(SessionService sessionService) : IStripeService
{
    public async Task<(string ProviderReference, string CheckoutUrl)> CreateCheckoutSessionAsync(
        Guid invoiceId,
        decimal amountDue,
        string successUrl,
        string cancelUrl,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        // 1. Prepare Stripe Line Item (What the customer sees on the Checkout page)
        // Note: Stripe expects amounts in the smallest currency unit (cents/grosze).
        // e.g., 100.00 PLN -> 10000 groszy.
        var amountInCents = (long)Math.Round(amountDue * 100m, MidpointRounding.AwayFromZero);

        var options = new SessionCreateOptions
        {
            // Explicitly defining methods ensures your C# code is the single source of truth
            // and perfectly compatible with your current Stripe.net SDK version.
            PaymentMethodTypes = new List<string> { "card", "blik" },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            ClientReferenceId = invoiceId.ToString(), // Helps track the invoice on the Stripe Dashboard manually
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "pln",
                        UnitAmount = amountInCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Invoice {invoiceId.ToString()[..8].ToUpper()}",
                            Description = "Payment for corporate billing services."
                        }
                    },
                    Quantity = 1,
                }
            },
            // CRITICAL: Associate this session with an invoice so we can reliably map it back inside the webhook payload
            Metadata = new Dictionary<string, string>
            {
                { "InvoiceId", invoiceId.ToString() }
            }
        };

        // 2. Setup Idempotency for the API request
        // This prevents Stripe from charging the customer or creating duplicate sessions 
        // if our server crashes right after sending the request but before receiving the response.
        var requestOptions = new RequestOptions
        {
            IdempotencyKey = idempotencyKey
        };

        // 3. Make the External API Call with Infrastructure Error Translation
        try
        {
            var session = await sessionService.CreateAsync(options, requestOptions, cancellationToken);

            // 4. Return agnostic details to the Application layer
            return (session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            // Translate provider-specific errors into our generic infrastructure exception.
            // Bulletproof null-safety for HTTP Status Code.
            var statusCode = (int?)ex.HttpStatusCode ?? 0;

            // Network errors (0) or Stripe server errors (5xx) are transient.
            // Validation errors (4xx) are definitive.
            bool isTransient = statusCode >= 500 || statusCode == 0;

            throw new ExternalServiceException(
                $"Gateway Error: {ex.StripeError?.Message ?? ex.Message}",
                isTransient,
                ex);
        }
    }
}
