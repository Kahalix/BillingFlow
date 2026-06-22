using BillingFlow.Application.Interfaces;

namespace BillingFlow.Application.Features.Identity.IntegrationEvents;

/// <summary>
/// An integration event representing the intent to send a security alert to a user's previous email address.
/// Persisted durably within the Outbox log for asynchronous, resilient processing.
/// </summary>
public record SendSecurityNoticeOnEmailChangedEvent(string OldEmail) : IIntegrationEvent;
