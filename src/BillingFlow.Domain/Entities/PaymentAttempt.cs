using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// Tracks an asynchronous online payment attempt.
/// Agnostic to the specific gateway (Stripe, PayPal, etc.) to ensure the Domain is future-proof.
/// Implements the Reservation pattern to prevent external API race conditions.
/// </summary>
public class PaymentAttempt : Entity, IAggregateRoot
{
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentProvider Provider { get; private set; }

    // Fully agnostic reference (e.g., Stripe sessionId, PayPal orderId, Adyen pspReference).
    // Nullable initially during the DB "Reservation" phase before the API call succeeds.
    public string? ProviderReference { get; private set; }
    public string? CheckoutUrl { get; private set; }

    public PaymentStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // Session expiration to prevent returning dead checkout links to the client
    // and to free up the DB lock for future attempts.
    public DateTimeOffset ExpiresAt { get; private set; }

    protected PaymentAttempt() { }

    private PaymentAttempt(
        Guid invoiceId, decimal amount, PaymentProvider provider,
        DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Id = Guid.NewGuid();
        InvoiceId = invoiceId;
        Amount = amount;
        Provider = provider;
        Status = PaymentStatus.Initializing; // START IN INITIALIZING STATE
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Phase 1: Reserve the intent in the database to lock the invoice and prevent race conditions.
    /// Does not require external API details yet.
    /// </summary>
    public static PaymentAttempt Reserve(
        Guid invoiceId, decimal amount, PaymentProvider provider,
        DateTimeOffset now, DateTimeOffset expiresAt)
    {
        if (invoiceId == Guid.Empty) throw new DomainException("Invoice ID is required.");
        if (amount <= 0) throw new DomainException("Amount must be greater than zero.");
        if (provider == PaymentProvider.BackOffice) throw new DomainException("BackOffice payments do not require tracking an attempt.");
        if (expiresAt <= now) throw new DomainException("Expiration date must be in the future.");

        return new PaymentAttempt(invoiceId, amount, provider, now, expiresAt);
    }

    /// <summary>
    /// Phase 2: Populate the session details after a successful gateway API call.
    /// </summary>
    public void SetCheckoutDetails(string providerReference, string checkoutUrl)
    {
        if (Status != PaymentStatus.Initializing)
            throw new DomainException("Cannot set checkout details unless the attempt is initializing.");

        if (string.IsNullOrWhiteSpace(providerReference)) throw new DomainException("Provider Reference is required.");
        if (string.IsNullOrWhiteSpace(checkoutUrl)) throw new DomainException("Checkout URL is required.");

        ProviderReference = providerReference;
        CheckoutUrl = checkoutUrl;

        // Explicit state transition
        Status = PaymentStatus.Started;
    }

    /// <summary>
    /// Frees up the database concurrency lock.
    /// Also sanitizes the URL to ensure it cannot be accidentally reused or leaked.
    /// </summary>
    public void MarkAsExpired()
    {
        if (Status is not (PaymentStatus.Started or PaymentStatus.Initializing)) return;

        Status = PaymentStatus.Expired;
        CheckoutUrl = null;
    }

    public void MarkAsSucceeded()
    {
        if (Status == PaymentStatus.Succeeded) return;
        if (Status == PaymentStatus.Failed) throw new DomainException("Cannot succeed an attempt that already failed.");

        Status = PaymentStatus.Succeeded;
    }

    public void MarkAsFailed(string errorReason)
    {
        if (Status == PaymentStatus.Succeeded) throw new DomainException("Cannot fail an attempt that already succeeded.");

        Status = PaymentStatus.Failed;
        ErrorMessage = errorReason;

        // Data hygiene: ensure a failed attempt cannot leak a stale/invalid URL
        CheckoutUrl = null;
        ProviderReference = null; // Clean up provider reference
    }
}
