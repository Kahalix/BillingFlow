using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when a user successfully changes their password.
/// Useful for auditing and revoking existing active sessions.
/// </summary>
public record PasswordChangedEvent(Guid UserId) : IDomainEvent;
