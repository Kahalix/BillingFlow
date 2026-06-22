namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Marker interface for events that cross the bounded context boundary and require 
/// guaranteed, at-least-once delivery to external systems (e.g., email providers, message brokers).
/// </summary>
public interface IIntegrationEvent { }

/// <summary>
/// Abstracts the publication of integration events.
/// The underlying infrastructure implementation (Outbox Pattern) guarantees that the event 
/// is durably persisted within the exact same database transaction as the business state mutation.
/// </summary>
public interface IIntegrationEventPublisher
{
    void Publish<T>(T integrationEvent) where T : IIntegrationEvent;
}
