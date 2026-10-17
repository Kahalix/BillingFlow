using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Queries.GetClients;

public class GetClientsHandler(IApplicationDbContext context)
    : IQueryHandler<GetClientsQuery, PaginatedList<ClientSummaryDto>>
{
    public async Task<Result<PaginatedList<ClientSummaryDto>>> Handle(GetClientsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Clients.AsNoTracking();

        if (request.Status == ClientStatus.Archived)
        {
            query = query.IgnoreQueryFilters().Where(c => c.Status == ClientStatus.Archived);
        }
        else if (request.Status.HasValue)
        {
            query = query.Where(c => c.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(c =>
                c.CompanyName.Contains(term) ||
                c.TaxId.Contains(term));
        }

        query = query.OrderBy(c => c.CompanyName);

        var projectedQuery = query.Select(c => new ClientSummaryDto(
            c.Id,
            c.CompanyName,
            c.TaxId,
            c.Status
        ));

        var result = await PaginatedList<ClientSummaryDto>.CreateAsync(
            projectedQuery,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}
