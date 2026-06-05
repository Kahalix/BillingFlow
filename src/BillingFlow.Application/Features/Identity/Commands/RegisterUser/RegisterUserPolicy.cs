using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Authorization.Roles;
using BillingFlow.Application.Interfaces;

namespace BillingFlow.Application.Features.Identity.Commands.RegisterUser;

/// <summary>
/// Evaluates whether the currently authenticated user has the hierarchical authority 
/// to provision an account with the requested target role.
/// </summary>
public class RegisterUserPolicy(ICurrentUserService currentUserService) : IAuthorizationPolicy<RegisterUserCommand>
{
    public Task<bool> AuthorizeAsync(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var creatorRole = currentUserService.UserRole;
        var isAuthorized = RoleHierarchy.CanManageRole(creatorRole, request.TargetRole);

        return Task.FromResult(isAuthorized);
    }
}
