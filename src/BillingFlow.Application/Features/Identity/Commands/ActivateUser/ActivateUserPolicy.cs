// File: src/BillingFlow.Application/Features/Identity/Commands/ActivateUser/ActivateUserPolicy.cs
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Authorization.Roles;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.ActivateUser;

public class ActivateUserPolicy(
    ICurrentUserService currentUserService,
    IApplicationDbContext context) : IAuthorizationPolicy<ActivateUserCommand>
{
    public async Task<bool> AuthorizeAsync(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var currentUserRole = currentUserService.UserRole;

        // 1. Explicit business rule: You cannot activate yourself (even if unreachable via HTTP)
        if (currentUserId == request.UserId)
        {
            return false;
        }

        var targetUser = await context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Role })
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (targetUser is null) return true;

        // 2. Use the unified hierarchy check
        if (currentUserRole != Domain.Enums.Role.Admin && !RoleHierarchy.CanManageRole(currentUserRole, targetUser.Role))
        {
            return false;
        }

        return true;
    }
}
