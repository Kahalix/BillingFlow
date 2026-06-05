using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when a user's system role is updated.
/// </summary>
public record UserRoleChangedEvent(Guid UserId, Role OldRole, Role NewRole) : IDomainEvent;
