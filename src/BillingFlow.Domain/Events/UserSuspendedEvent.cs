// File: src/BillingFlow.Domain/Events/UserSuspendedEvent.cs
using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when a user account is suspended by an administrator.
/// </summary>
public record UserSuspendedEvent(Guid UserId) : IDomainEvent;
