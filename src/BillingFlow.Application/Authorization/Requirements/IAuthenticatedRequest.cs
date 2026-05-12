// File: src/BillingFlow.Application/Authorization/Requirements/IAuthenticatedRequest.cs
namespace BillingFlow.Application.Authorization.Requirements;

/// <summary>
/// Marker interface for requests that only require the user to be logged in, 
/// without needing specific static permissions or complex policies.
/// E.g., GetCurrentUserQuery, LogoutCommand.
/// </summary>
public interface IAuthenticatedRequest
{
}
