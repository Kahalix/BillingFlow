using System;
using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Interfaces;

public interface IStripeService
{
    /// <summary>
    /// Communicates with Provider API to create a Checkout Session.
    /// Returns the generated Stripe Session ID and the secure URL to redirect the user.
    /// </summary>
    Task<(string ProviderReference, string CheckoutUrl)> CreateCheckoutSessionAsync(
        Guid invoiceId,
        decimal amountDue,
        string successUrl,
        string cancelUrl,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
