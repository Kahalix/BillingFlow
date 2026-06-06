using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Authorization.Roles;

/// <summary>
/// Defines the mapping between domain roles and system permissions.
/// This acts as the central truth for what each role is allowed to do globally.
/// </summary>
public static class RolePermissions
{
    private static readonly IReadOnlyList<string> AdminPermissions =
    [
        AppPermissions.UsersCreate, AppPermissions.UsersRead,
        AppPermissions.UsersActivate, AppPermissions.UsersChangeRole,
        AppPermissions.UsersChangeEmail, AppPermissions.UsersSuspend,

        AppPermissions.ClientsRead, AppPermissions.ClientsArchive,
        AppPermissions.ClientsRestore,
        AppPermissions.ClientsCreate, AppPermissions.ClientsUpdate,
        AppPermissions.ClientsSuspend, AppPermissions.ClientsActivate,

        AppPermissions.InvoicesRead, AppPermissions.InvoicesGenerate,

        AppPermissions.ProvidedServicesRead, AppPermissions.ProvidedServicesCreate,
        AppPermissions.ProvidedServicesUpdate, AppPermissions.ProvidedServicesDelete,

        AppPermissions.ReportsRead,

        AppPermissions.PaymentsRead, AppPermissions.PaymentsCreate,
        AppPermissions.PaymentsCreateManual, AppPermissions.PaymentsRefund
    ];

    private static readonly IReadOnlyList<string> ManagerPermissions =
    [
        AppPermissions.UsersCreate, AppPermissions.UsersRead,
        AppPermissions.UsersChangeEmail, AppPermissions.UsersSuspend,
        AppPermissions.UsersActivate, AppPermissions.UsersChangeRole,

        AppPermissions.ClientsRead, AppPermissions.ClientsArchive,
        AppPermissions.ClientsRestore,
        AppPermissions.ClientsCreate, AppPermissions.ClientsUpdate,
        AppPermissions.ClientsSuspend, AppPermissions.ClientsActivate,

        AppPermissions.InvoicesRead, AppPermissions.InvoicesGenerate,
        AppPermissions.InvoicesCancel,

        AppPermissions.ProvidedServicesRead, AppPermissions.ProvidedServicesCreate,
        AppPermissions.ProvidedServicesUpdate, AppPermissions.ProvidedServicesDelete,

        AppPermissions.ReportsRead,

        AppPermissions.PaymentsRead, AppPermissions.PaymentsCreate,
        AppPermissions.PaymentsCreateManual, AppPermissions.PaymentsRefund

    ];

    private static readonly IReadOnlyList<string> AccountantPermissions =
    [
        AppPermissions.ProvidedServicesRead,

        AppPermissions.InvoicesRead, AppPermissions.InvoicesGenerate,
        AppPermissions.InvoicesCancel,

        AppPermissions.ReportsRead,

        AppPermissions.PaymentsRead, AppPermissions.PaymentsCreate,
        AppPermissions.PaymentsCreateManual, AppPermissions.PaymentsRefund
    ];

    private static readonly IReadOnlyList<string> EmployeePermissions =
    [
        AppPermissions.UsersCreate, AppPermissions.UsersRead,

        AppPermissions.ClientsRead, AppPermissions.ClientsUpdate,

        AppPermissions.ProvidedServicesRead, AppPermissions.ProvidedServicesCreate,
        AppPermissions.ProvidedServicesUpdate, AppPermissions.ProvidedServicesDelete,

        AppPermissions.InvoicesRead,

        AppPermissions.PaymentsRead, AppPermissions.PaymentsCreateManual
    ];

    private static readonly IReadOnlyList<string> CustomerPermissions =
    [
        // Static permissions act as "module entry pass". 
        // Data ownership (e.g., "can I read THIS specific invoice") must be validated by IAuthorizationPolicy.

        AppPermissions.ProvidedServicesRead,

        AppPermissions.InvoicesRead,

        AppPermissions.PaymentsRead,
        AppPermissions.PaymentsCreate
    ];

    /// <summary>
    /// Returns the list of permissions associated with a specific role.
    /// </summary>
    public static IReadOnlyList<string> GetPermissionsFor(Role role) => role switch
    {
        Role.Admin => AdminPermissions,
        Role.Manager => ManagerPermissions,
        Role.Accountant => AccountantPermissions,
        Role.Employee => EmployeePermissions,
        Role.Customer => CustomerPermissions,
        _ => []
    };
}
