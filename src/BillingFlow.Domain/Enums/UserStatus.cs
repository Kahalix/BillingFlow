// File: src/BillingFlow.Domain/Enums/UserStatus.cs
namespace BillingFlow.Domain.Enums;

/// <summary>
/// Represents the security and access state of a user account.
/// </summary>
public enum UserStatus
{
    Active = 1,
    Suspended = 2,
    Deactivated = 3
}
