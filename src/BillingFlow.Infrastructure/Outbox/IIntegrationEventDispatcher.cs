using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Infrastructure.Outbox;

/// <summary>
/// Decouples the Outbox Relay Worker from the actual transport mechanism (Hangfire, Kafka, RabbitMQ).
/// </summary>
public interface IIntegrationEventDispatcher
{
    Task DispatchAsync(Guid outboxMessageId, string eventType, string payload, CancellationToken cancellationToken);
}
