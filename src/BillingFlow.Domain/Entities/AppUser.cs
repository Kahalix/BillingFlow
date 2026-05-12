// File: src/BillingFlow.Domain/Entities/AppUser.cs
using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// Aggregate Root representing a system user and their identity credentials.
/// </summary>
public class AppUser : Entity, IAggregateRoot
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Role Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    protected AppUser() { }

    public AppUser(string email, string passwordHash, Role role, DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty.");

        if (!Enum.IsDefined(typeof(Role), role))
            throw new DomainException("Invalid system role provided.");

        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = createdAt;
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash cannot be empty.");

        PasswordHash = newPasswordHash;
    }

    /// <summary>
    /// Updates the last login timestamp with basic invariant validation.
    /// </summary>
    public void RecordLogin(DateTime loggedInAt)
    {
        if (loggedInAt < CreatedAt)
            throw new DomainException("Login time cannot be earlier than account creation time.");

        LastLoginAt = loggedInAt;
    }
}
