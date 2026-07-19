using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Queries.GetCurrentUser;

/// <summary>
/// Retrieves the full profile of the currently authenticated user.
/// Requires the user to be logged in, but no specific permissions are needed.
/// </summary>
public record GetCurrentUserQuery : IQuery<CurrentUserProfileDto>, IAuthenticatedRequest;
