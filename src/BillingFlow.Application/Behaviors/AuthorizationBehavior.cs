using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Results;
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
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // 1. Bypass authorization for explicitly public requests
        if (request is IPublicRequest) return await next(cancellationToken);

        // 2. CRITICAL FAIL-FAST LOGIC: Ensure the request declares a security model.
        // Prevents accidentally exposing endpoints without authorization.
        bool isExplicitlySecured = request is IAuthenticatedRequest ||
                                   request is IRequirePermission ||
                                   request is IRequirePolicy;

        if (!isExplicitlySecured)
        {
            throw new InvalidOperationException(
                $"Security Configuration Error: '{typeof(TRequest).Name}' must implement one of the security markers: " +
                $"IPublicRequest, IAuthenticatedRequest, IRequirePermission, or IRequirePolicy.");
        }

        // 3. Enforce basic authentication (Returns HTTP 401 via Result)
        if (!currentUserService.IsAuthenticated || currentUserService.UserId == Guid.Empty)
        {
            return TResponse.Failure(Errors.Auth.Unauthorized);
        }

        // 4. Enforce specific policy if requested (Returns HTTP 403 via Result)
        if (request is IRequirePolicy)
        {
            // Fail-fast if multiple policies are accidentally registered
            var policy = policies.SingleOrDefault();

            if (policy is null)
                throw new InvalidOperationException($"Security Configuration Error: No IAuthorizationPolicy registered for '{typeof(TRequest).Name}'.");

            if (!await policy.AuthorizeAsync(request, cancellationToken))
            {
                return TResponse.Failure(Errors.Auth.Forbidden);
            }
        }

        return await next(cancellationToken);
    }
}
