// File: src/BillingFlow.Application/Features/Identity/Queries/GetCurrentUser/GetCurrentUserQuery.cs
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Queries.GetCurrentUser;

/// <summary>
/// Retrieves the full profile of the currently authenticated user.
/// Requires the user to be logged in, but no specific permissions are needed.
/// </summary>
public record GetCurrentUserQuery : IRequest<CurrentUserProfileDto>, IAuthenticatedRequest;
