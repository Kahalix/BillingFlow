// File: src/BillingFlow.Infrastructure/Authorization/PermissionAuthorizationHandler.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Authorization.Roles;

using Microsoft.AspNetCore.Authorization;

namespace BillingFlow.Infrastructure.Authorization;

/// <summary>
/// ASP.NET Core AuthorizationHandler that checks if the current user's role 
/// contains the required permission for the requested endpoint.
/// </summary>
public class PermissionAuthorizationHandler(ICurrentUserService currentUserService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!currentUserService.IsAuthenticated)
        {
            return Task.CompletedTask;
        }

        // Fetch permissions for the user's role
        var permissions = RolePermissions.GetPermissionsFor(currentUserService.UserRole);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
