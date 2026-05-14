// File: src/BillingFlow.Application/Authorization/Roles/RoleHierarchy.cs
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Authorization.Roles;

/// <summary>
/// Defines the explicit hierarchy matrix for role provisioning.
/// </summary>
public static class RoleHierarchy
{
    private static readonly IReadOnlyDictionary<Role, HashSet<Role>> AllowedHierarchy = new Dictionary<Role, HashSet<Role>>
    {
        { Role.Admin, [Role.Admin, Role.Manager, Role.Accountant, Role.Employee, Role.Customer] },
        { Role.Manager, [Role.Accountant, Role.Employee, Role.Customer] },
        { Role.Accountant, [Role.Employee, Role.Customer] },
        { Role.Employee, [Role.Customer] },
        { Role.Customer, [] }
    };

    /// <summary>
    /// Determines if the actor role has the hierarchical authority to manage 
    /// (provision, suspend, activate, edit) the target role.
    /// </summary>
    public static bool CanManageRole(Role actorRole, Role targetRole) => 
        AllowedHierarchy.TryGetValue(actorRole, out var allowed) && allowed.Contains(targetRole);
}
