using System;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// A clean, framework-agnostic DTO containing the extracted payment event data.
/// </summary>
public record PaymentCompletedEventDto(
    string EventId,
    string ProviderReference,
    Guid InvoiceId,
    PaymentMethod Method);

/// <summary>
/// Abstracts the cryptographic validation of webhook payloads.
/// </summary>
public interface IStripeWebhookValidator
{
    /// <summary>
    /// Validates the cryptographic signature of the webhook and extracts relevant payment data.
    /// Returns the parsed payload if the event is a successful checkout.
    /// Returns null if the signature is valid but the event type is not relevant (e.g., customer.created).
    /// </summary>
    PaymentCompletedEventDto? ValidateAndParse(string jsonPayload, string signatureHeader);
}
