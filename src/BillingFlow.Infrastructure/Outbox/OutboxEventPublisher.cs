using System;
using System.Text.Json;

using BillingFlow.Application.Interfaces;
using BillingFlow.Infrastructure.Database;
using BillingFlow.Infrastructure.Outbox.Models;

namespace BillingFlow.Infrastructure.Outbox;

/// <summary>
/// Infrastructure-specific implementation of the integration event publisher.
/// Serializes events and appends them to the EF Core change tracker.
/// </summary>
public class OutboxEventPublisher(
    BillingDbContext context,
    TimeProvider timeProvider) : IIntegrationEventPublisher
{
    public void Publish<T>(T integrationEvent) where T : IIntegrationEvent
    {
        var outboxMessage = new OutboxMessage(
            type: integrationEvent.GetType().Name,
            payload: JsonSerializer.Serialize(integrationEvent),
            occurredOn: timeProvider.GetUtcNow()
        );

        // Appends to the current EF Core ChangeTracker.
        // Will be atomically committed alongside domain entity mutations when SaveChangesAsync is called.
        context.OutboxMessages.Add(outboxMessage);
    }
}
