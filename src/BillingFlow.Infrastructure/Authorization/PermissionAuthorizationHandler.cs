using BillingFlow.Application.Authorization;

using Microsoft.AspNetCore.Authorization;

namespace BillingFlow.Infrastructure.Authorization;

/// <summary>
/// ASP.NET Core AuthorizationHandler that checks if the current user's JWT 
/// contains the required permission claim for the requested endpoint.
/// Completely decoupled from roles and database.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Simply check if the token contains the specific permission claim
        if (context.User.HasClaim(CustomClaimTypes.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
