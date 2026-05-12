// File: src/BillingFlow.Application/Authorization/Permissions/Permissions.cs
namespace BillingFlow.Application.Authorization.Permissions;

/// <summary>
/// Contains all fine-grained permissions available in the system.
/// Handlers and Policies should check against these constants, NOT against Roles.
/// </summary>
public static class AppPermissions
{
    // Clients
    public const string ClientsRead = "clients.read";
    public const string ClientsCreate = "clients.create";
    public const string ClientsSuspend = "clients.suspend";

    // Invoices
    public const string InvoicesRead = "invoices.read";
    public const string InvoicesGenerate = "invoices.generate";

    // Payments
    public const string PaymentsRead = "payments.read";
    public const string PaymentsRefund = "payments.refund";
}
