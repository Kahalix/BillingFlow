using BillingFlow.Application.Interfaces;

namespace BillingFlow.Application.Features.Clients.IntegrationEvents;

/// <summary>
/// An integration event representing the intent to notify a suspended client.
/// This event will be serialized into the Outbox and processed asynchronously by background workers.
/// </summary>
public record SendClientSuspensionNoticeEvent(
    string Email,
    string CompanyName) : IIntegrationEvent;
