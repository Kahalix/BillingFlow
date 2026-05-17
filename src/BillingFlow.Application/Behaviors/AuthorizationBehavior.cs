// File: src/BillingFlow.Application/Behaviors/AuthorizationBehavior.cs
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Authorization.Services;
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

namespace BillingFlow.Application.Behaviors;

/// <summary>
/// Intercepts all MediatR requests to enforce fail-secure authorization rules.
/// Must be registered as the FIRST behavior in the MediatR pipeline.
/// </summary>
public class AuthorizationBehavior<TRequest, TResponse>(
    ICurrentUserService currentUserService,
    IAuthorizer authorizer)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 1. Fully Public Endpoint
        if (request is IPublicRequest)
        {
            return await next(cancellationToken);
        }

        // If it's not public, the user MUST be authenticated.
        if (!currentUserService.IsAuthenticated)
        {
            // Throwing an Unauthorized exception (which API will map to HTTP 401)
            throw new UnauthorizedException("Authentication is required to access this resource.");
        }

        bool isExplicitlySecured = false;

        // 2. Requires basic authentication only
        if (request is IAuthenticatedRequest)
        {
            isExplicitlySecured = true;
        }

        // 3. Static Permission Check
        if (request is IRequirePermission permissionRequest)
        {
            isExplicitlySecured = true;
            if (!await authorizer.HasPermissionAsync(permissionRequest.Permission, cancellationToken))
            {
                throw new ForbiddenException($"Missing required permission: {permissionRequest.Permission}");
            }
        }

        // 4. Dynamic Policy Check
        if (request is IRequirePolicy)
        {
            isExplicitlySecured = true;
            if (!await authorizer.IsAuthorizedByPolicyAsync(request, cancellationToken))
            {
                throw new ForbiddenException("Policy requirements not met for this resource.");
            }
        }

        // 5. Fail-Secure Enforcement
        // Prevents developers from creating a MediatR request without defining its security boundaries.
        if (!isExplicitlySecured)
        {
            throw new InvalidOperationException(
                $"Security Configuration Error: '{typeof(TRequest).Name}' must implement one of the security markers: " +
                $"IPublicRequest, IAuthenticatedRequest, IRequirePermission, or IRequirePolicy.");
        }

        // All checks passed, continue pipeline
        return await next(cancellationToken);
    }
}
