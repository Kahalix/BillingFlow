using BillingFlow.Application.Interfaces;

namespace BillingFlow.Application.Features.Identity.IntegrationEvents;

/// <summary>
/// An integration event representing the intent to send a password reset link to a user.
/// Persisted durably within the Outbox log for asynchronous, resilient processing.
/// </summary>
public record SendPasswordResetEmailEvent(string Email, string RawToken) : IIntegrationEvent;
