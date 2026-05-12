// File: src/BillingFlow.Application/Authorization/Requirements/IAuthorizationPolicy.cs
namespace BillingFlow.Application.Authorization.Requirements;

/// <summary>
/// Defines a dynamic, resource-based or data-ownership authorization policy for a specific request.
/// Implement this interface to create policies like "GetInvoiceDetailsPolicy".
/// </summary>
public interface IAuthorizationPolicy<in TRequest> where TRequest : notnull
{
    /// <summary>
    /// Evaluates if the current context meets the policy requirements.
    /// </summary>
    Task<bool> CanExecuteAsync(TRequest request, CancellationToken cancellationToken);
}
