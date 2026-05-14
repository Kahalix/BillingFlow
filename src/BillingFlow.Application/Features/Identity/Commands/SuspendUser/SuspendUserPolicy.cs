// File: src/BillingFlow.Application/Features/Identity/Commands/SuspendUser/SuspendUserPolicy.cs
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Authorization.Roles;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.SuspendUser;

/// <summary>
/// Evaluates if the current user has the hierarchical authority to suspend the target user.
/// </summary>
public class SuspendUserPolicy(
    ICurrentUserService currentUserService,
    IApplicationDbContext context) : IAuthorizationPolicy<SuspendUserCommand>
{
    public async Task<bool> CanExecuteAsync(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var currentUserRole = currentUserService.UserRole;

        // 1. Prevent self-suspension (a user cannot lock themselves out)
        if (currentUserId == request.UserId)
        {
            return false;
        }

        // 2. Fetch the target user's role
        // We only fetch the ID and Role to keep the query lightweight.
        var targetUser = await context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Role })
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        // If the user doesn't exist, let the policy pass so the Handler can throw a proper NotFoundException.
        if (targetUser is null)
        {
            return true;
        }

        // 3. Enforce Hierarchical Rules
        // We reuse the RoleHierarchy matrix. An Employee cannot suspend a Manager, etc.
        // Exception: Admins can suspend other Admins.
        if (currentUserRole != Domain.Enums.Role.Admin && !RoleHierarchy.CanManageRole(currentUserRole, targetUser.Role))
        {
            return false;
        }

        return true;
    }
}
