// File: src/BillingFlow.Application/Authorization/Requirements/IRequirePermission.cs
namespace BillingFlow.Application.Authorization.Requirements;

/// <summary>
/// Implement this interface on a MediatR request to enforce a static permission check.
/// </summary>
public interface IRequirePermission
{
    string Permission { get; }
}
