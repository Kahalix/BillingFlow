// File: src/BillingFlow.Infrastructure/Authorization/PermissionClaimsProvider.cs
using System.Security.Claims;

using BillingFlow.Application.Authorization;
using BillingFlow.Application.Authorization.Roles;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

namespace BillingFlow.Infrastructure.Authorization;

public class PermissionClaimsProvider : IPermissionClaimsProvider
{
    public IEnumerable<Claim> GetClaimsForRole(Role role)
    {
        var permissions = RolePermissions.GetPermissionsFor(role) ?? Enumerable.Empty<string>();

        // Distinct() ensures no duplicate claims are added to the JWT
        return permissions
            .Distinct()
            .Select(permission => new Claim(CustomClaimTypes.Permission, permission));
    }
}
