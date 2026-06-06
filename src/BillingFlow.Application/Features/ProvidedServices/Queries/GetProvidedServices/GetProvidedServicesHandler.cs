using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServices;

public class GetProvidedServicesHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetProvidedServicesQuery, PaginatedList<ProvidedServiceSummaryDto>>
{
    public async Task<PaginatedList<ProvidedServiceSummaryDto>> Handle(GetProvidedServicesQuery request, CancellationToken cancellationToken)
    {
        var query = from service in context.ProvidedServices.AsNoTracking()
                    join client in context.Clients.IgnoreQueryFilters() on service.ClientId equals client.Id
                    select new { service, client };

        // Row-Level Security explicitly for the Customer scope
        if (currentUserService.UserRole == Role.Customer)
        {
            query = query.Where(x => x.client.UserId == currentUserService.UserId);
        }

        if (request.FilterByClientId.HasValue)
            query = query.Where(x => x.service.ClientId == request.FilterByClientId.Value);

        if (request.FilterByStatus.HasValue)
            query = query.Where(x => x.service.Status == request.FilterByStatus.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim();
            query = query.Where(x => x.service.Description.Contains(search) ||
                                     x.client.CompanyName.Contains(search));
        }

        var projectedQuery = query
            .OrderByDescending(x => x.service.PerformedAt)
            .Select(x => new ProvidedServiceSummaryDto(
                x.service.Id,
                x.service.ClientId,
                x.client.CompanyName,
                x.service.Description,
                x.service.Amount,
                x.service.PerformedAt,
                x.service.InvoiceId,
                x.service.Status
            ));

        return await PaginatedList<ProvidedServiceSummaryDto>.CreateAsync(
            projectedQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
