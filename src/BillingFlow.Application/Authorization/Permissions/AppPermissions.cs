// File: src/BillingFlow.Application/Authorization/Permissions/AppPermissions.cs
namespace BillingFlow.Application.Authorization.Permissions;

/// <summary>
/// Contains all fine-grained permissions available in the system.
/// Handlers and Policies should check against these constants, NOT against Roles.
/// </summary>
public static class AppPermissions
{
    // AppUsers
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersSuspend = "users.suspend";
    public const string UsersActivate = "users.activate";
    public const string UsersChangeRole = "users.changeRole";
    public const string UsersChangeEmail = "users.changeEmail";

    // Clients
    public const string ClientsRead = "clients.read";
    public const string ClientsCreate = "clients.create";
    public const string ClientsSuspend = "clients.suspend";
    public const string ClientsActivate = "clients.activate";
    public const string ClientsArchive = "clients.archive";
    public const string ClientsUpdate = "clients.update";
    public const string ClientsRestore = "clients.restore";

    // Invoices
    public const string InvoicesRead = "invoices.read";
    public const string InvoicesGenerate = "invoices.generate";

    // Payments
    public const string PaymentsRead = "payments.read";
    public const string PaymentsRefund = "payments.refund";
}
