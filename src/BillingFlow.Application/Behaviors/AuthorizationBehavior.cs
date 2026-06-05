using BillingFlow.Application.Authorization.Requirements;
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
    IEnumerable<IAuthorizationPolicy<TRequest>> policies)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IPublicRequest) return await next();

        if (!currentUserService.IsAuthenticated || currentUserService.UserId == Guid.Empty)
        {
            throw new UnauthorizedException("Authentication is required to access this resource.");
        }

        bool isExplicitlySecured = false;

        if (request is IAuthenticatedRequest || request is IRequirePermission)
        {
            isExplicitlySecured = true;
        }

        if (request is IRequirePolicy)
        {
            isExplicitlySecured = true;

            // Use SingleOrDefault to fail-fast if multiple policies are accidentally registered
            var policy = policies.SingleOrDefault();

            if (policy is null)
            {
                throw new InvalidOperationException($"Security Configuration Error: No IAuthorizationPolicy registered for '{typeof(TRequest).Name}'.");
            }

            if (!await policy.AuthorizeAsync(request, cancellationToken))
            {
                throw new ForbiddenException("Policy requirements not met for this resource.");
            }
        }

        if (!isExplicitlySecured)
        {
            throw new InvalidOperationException(
                $"Security Configuration Error: '{typeof(TRequest).Name}' must implement one of the security markers: " +
                $"IPublicRequest, IAuthenticatedRequest, IRequirePermission, or IRequirePolicy.");
        }

        return await next();
    }
}
