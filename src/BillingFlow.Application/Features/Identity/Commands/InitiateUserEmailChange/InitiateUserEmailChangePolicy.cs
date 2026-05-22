// File: src/BillingFlow.Application/Features/Identity/Commands/InitiateUserEmailChange/InitiateUserEmailChangePolicy.cs
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Authorization.Roles;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateUserEmailChange;

public class InitiateUserEmailChangePolicy(
    ICurrentUserService currentUserService,
    IApplicationDbContext context) : IAuthorizationPolicy<InitiateUserEmailChangeCommand>
{
    public async Task<bool> AuthorizeAsync(InitiateUserEmailChangeCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var currentUserRole = currentUserService.UserRole;

        if (currentUserId == request.TargetUserId) return false; // Self-service should be used

        var targetUser = await context.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Role })
            .SingleOrDefaultAsync(u => u.Id == request.TargetUserId, cancellationToken);

        if (targetUser is null) return true; // Let handler throw NotFound

        // CRITICAL BUSINESS RULE: Only Admin can change emails of non-customer roles
        if (targetUser.Role != Domain.Enums.Role.Customer && currentUserRole != Domain.Enums.Role.Admin)
        {
            return false;
        }

        return RoleHierarchy.CanManageRole(currentUserRole, targetUser.Role);
    }
}
