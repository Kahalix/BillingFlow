using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Queries.GetUserById;

/// <summary>
/// Retrieves detailed profile information for a specific user by their ID.
/// Requires administrative read access.
/// </summary>
public record GetUserByIdQuery(Guid UserId) : IRequest<UserDetailsDto>, IRequirePermission;
