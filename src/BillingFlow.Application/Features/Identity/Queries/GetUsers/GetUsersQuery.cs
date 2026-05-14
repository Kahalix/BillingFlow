// File: src/BillingFlow.Application/Features/Identity/Queries/GetUsers/GetUsersQuery.cs
using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Models;
using BillingFlow.Domain.Enums;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Queries.GetUsers;

/// <summary>
/// Retrieves a paginated and optionally filtered list of users.
/// Requires basic administrative read permissions.
/// </summary>
public record GetUsersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Role? FilterByRole = null,
    string? SearchTerm = null) : IRequest<PaginatedList<UserSummaryDto>>, IRequirePermission
{
    // Assuming you add AppPermissions.UsersRead = "users.read"
    public string Permission => AppPermissions.UsersRead;
}
