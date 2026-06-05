using System;

using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when a suspended client is re-activated.
/// </summary>
public record ClientActivatedEvent(Guid ClientId) : IDomainEvent;
