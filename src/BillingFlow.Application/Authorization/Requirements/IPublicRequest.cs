namespace BillingFlow.Application.Authorization.Requirements;

/// <summary>
/// Marker interface for requests that can be accessed without any authentication.
/// E.g., LoginCommand, RegisterUserCommand.
/// </summary>
public interface IPublicRequest
{
}
