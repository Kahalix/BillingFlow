using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when a user successfully confirms their new email address.
/// Used to trigger a security notification to the old email.
/// </summary>
public record EmailChangedEvent(Guid UserId, string OldEmail, string NewEmail) : IDomainEvent;
