using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Authorization.Roles;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;

/// <summary>
/// Evaluates if the current user has the hierarchical authority to modify the target user's role.
/// This policy performs a "Double Check": verifying the actor's authority over both the TARGET'S CURRENT role and the NEW role.
/// </summary>
public class ChangeUserRolePolicy(
    ICurrentUserService currentUserService,
    IApplicationDbContext context) : IAuthorizationPolicy<ChangeUserRoleCommand>
{
    public async Task<bool> AuthorizeAsync(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var currentUserRole = currentUserService.UserRole;

        // 1. Lockout Prevention: Explicit business rule preventing users from altering their own role.
        // This ensures an Admin cannot accidentally demote themselves and lose access.
        if (currentUserId == request.UserId)
        {
            return false;
        }

        // 2. Privilege Escalation Check (New Role): Check if the actor is allowed to ASSIGN the requested new role.
        if (currentUserRole != Domain.Enums.Role.Admin && !RoleHierarchy.CanManageRole(currentUserRole, request.NewRole))
        {
            return false;
        }

        // Fetch the target user's current role lightly
        var targetUser = await context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Role })
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        // If the user doesn't exist, let the handler process it and throw a clean NotFoundException.
        if (targetUser is null) return true;

        // 3. Hierarchical Modification Check (Current Role): Check if the actor is allowed to MODIFY the target's current role.
        if (currentUserRole != Domain.Enums.Role.Admin && !RoleHierarchy.CanManageRole(currentUserRole, targetUser.Role))
        {
            return false;
        }

        return true;
    }
}
