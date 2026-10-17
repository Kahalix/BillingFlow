using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Queries.GetUsers;

public class GetUsersHandler(IApplicationDbContext context)
    : IQueryHandler<GetUsersQuery, PaginatedList<UserSummaryDto>>
{
    public async Task<Result<PaginatedList<UserSummaryDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        // 1. Start with an untracked queryable
        var query = context.Users.AsNoTracking();

        // 2. Apply filters dynamically
        if (request.FilterByRole.HasValue)
        {
            query = query.Where(u => u.Role == request.FilterByRole.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Case-insensitive search on the email
            var normalizedSearch = request.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(u => u.Email.Contains(normalizedSearch));
        }

        // 3. Define the projection (Select) early to optimize SQL execution
        var projectedQuery = query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Email,
                u.Role,
                u.Status,
                u.CreatedAt));

        // 4. Execute pagination
        var paginatedList = await PaginatedList<UserSummaryDto>.CreateAsync(
            projectedQuery,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return Result.Success(paginatedList);
    }
}
