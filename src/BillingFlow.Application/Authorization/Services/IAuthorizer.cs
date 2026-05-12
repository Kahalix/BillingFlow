// File: src/BillingFlow.Application/Authorization/Services/IAuthorizer.cs
namespace BillingFlow.Application.Authorization.Services;

/// <summary>
/// Core authorization service boundary in the Application layer.
/// Used by the Pipeline Behavior to evaluate both static permissions and dynamic policies.
/// </summary>
public interface IAuthorizer
{
    Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default);
    Task<bool> IsAuthorizedByPolicyAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : notnull;
}
