// File: src/BillingFlow.Application/Authorization/Services/Authorizer.cs
using BillingFlow.Application.Authorization.Roles;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Interfaces;

using Microsoft.Extensions.DependencyInjection;

namespace BillingFlow.Application.Authorization.Services;

/// <summary>
/// Central authorization engine. 
/// Designed with a fail-secure approach (defaults to deny if configuration is missing).
/// </summary>
public class Authorizer(ICurrentUserService currentUserService, IServiceProvider serviceProvider) : IAuthorizer
{
    public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.IsAuthenticated)
            return Task.FromResult(false);

        var userPermissions = RolePermissions.GetPermissionsFor(currentUserService.UserRole);
        return Task.FromResult(userPermissions.Contains(permission));
    }

    public async Task<bool> IsAuthorizedByPolicyAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : notnull
    {
        // Try to resolve the policy from DI
        var policy = serviceProvider.GetService<IAuthorizationPolicy<TRequest>>();

        // FAIL-SECURE: If a request relies on dynamic policy, but no policy is registered, 
        // it is a configuration error. We do NOT fallback to true.
        if (policy is null)
        {
            throw new InvalidOperationException($"Security Configuration Error: No IAuthorizationPolicy registered for '{typeof(TRequest).Name}'.");
        }

        // Execute the injected policy
        return await policy.CanExecuteAsync(request, cancellationToken);
    }
}
