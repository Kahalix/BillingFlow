namespace BillingFlow.Domain.Enums;

/// <summary>
/// Defines the core access control roles within the system.
/// This enum is the only authorization concept that exists in the Domain layer.
/// Specific access rights are resolved in the Application layer via Permissions.
/// </summary>
public enum Role
{
    Admin = 1,
    Manager = 2,
    Accountant = 3,
    Employee = 4,
    Customer = 5
}
