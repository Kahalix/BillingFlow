using System;

using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when a client is suspended.
/// </summary>
public record ClientSuspendedEvent(Guid ClientId) : IDomainEvent;
