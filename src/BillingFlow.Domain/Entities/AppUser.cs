// File: src/BillingFlow.Domain/Entities/AppUser.cs
using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Events;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// Aggregate Root representing a system user and their identity credentials.
/// Responsible for access control, authentication boundaries, and state mutations 
/// triggering domain events for side-effects.
/// </summary>
public class AppUser : Entity, IAggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Role Role { get; private set; }
    public UserStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    protected AppUser() { }

    public AppUser(string email, string passwordHash, Role role, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty.");

        if (!Enum.IsDefined(typeof(Role), role))
            throw new DomainException("Invalid system role provided.");

        Id = Guid.NewGuid();

        // Domain invariant: Emails are always stored in a normalized format.
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        Status = UserStatus.Active;
        CreatedAt = createdAt;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash cannot be empty.");

        PasswordHash = newPasswordHash;

        AddDomainEvent(new PasswordChangedEvent(Id));
    }

    public void RecordLogin(DateTimeOffset loggedInAt)
    {
        if (loggedInAt < CreatedAt)
            throw new DomainException("Login time cannot be earlier than account creation time.");

        LastLoginAt = loggedInAt;
    }

    public void Suspend()
    {
        if (Status == UserStatus.Suspended) return;

        Status = UserStatus.Suspended;

        // Emit domain event for asynchronous side-effects (e.g., revoking tokens)
        AddDomainEvent(new UserSuspendedEvent(Id));
    }

    public void Activate()
    {
        if (Status == UserStatus.Active) return;

        Status = UserStatus.Active;

        // Emit domain event to notify the system of reactivation
        AddDomainEvent(new UserActivatedEvent(Id));
    }

    public void ChangeRole(Role newRole)
    {
        if (!Enum.IsDefined(typeof(Role), newRole))
            throw new DomainException("Invalid system role provided.");

        if (Role == newRole) return;

        var oldRole = Role;
        Role = newRole;

        // Emits an event allowing the system to react (e.g., auditing, or session invalidation)
        AddDomainEvent(new UserRoleChangedEvent(Id, oldRole, newRole));
    }
}
