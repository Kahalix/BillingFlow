using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when a suspended user account is reactivated by an administrator.
/// </summary>
public record UserActivatedEvent(Guid UserId) : IDomainEvent;
