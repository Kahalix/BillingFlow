using BillingFlow.Application.Interfaces;

namespace BillingFlow.Application.Features.Identity.IntegrationEvents;

/// <summary>
/// An integration event representing the intent to send an email confirmation link to a new address.
/// Persisted durably within the Outbox log for asynchronous, resilient processing.
/// </summary>
public record SendEmailChangeConfirmationEvent(string NewEmail, string RawToken) : IIntegrationEvent;
