using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Invoices.Queries.GetInvoices;

public class GetInvoicesHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IQueryHandler<GetInvoicesQuery, PaginatedList<InvoiceSummaryDto>>
{
    public async Task<Result<PaginatedList<InvoiceSummaryDto>>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var query = from invoice in context.Invoices.AsNoTracking()
                    join client in context.Clients.IgnoreQueryFilters() on invoice.ClientId equals client.Id
                    select new { invoice, client };

        // Row-Level Security explicitly for the Customer scope
        if (currentUserService.UserRole == Role.Customer)
        {
            query = query.Where(x => x.invoice.OwnerUserId == currentUserService.UserId);
        }

        if (request.FilterByClientId.HasValue)
            query = query.Where(x => x.invoice.ClientId == request.FilterByClientId.Value);

        if (request.FilterByStatus.HasValue)
            query = query.Where(x => x.invoice.Status == request.FilterByStatus.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim();
            query = query.Where(x => x.invoice.InvoiceNumber.Contains(search) ||
                                     x.client.CompanyName.Contains(search));
        }

        var projectedQuery = query
            .OrderByDescending(x => x.invoice.IssueDate)
            .Select(x => new InvoiceSummaryDto(
                x.invoice.Id, x.invoice.InvoiceNumber, x.invoice.ClientId,
                x.client.CompanyName, x.invoice.TotalAmount, x.invoice.PaidAmount,
                x.invoice.IssueDate, x.invoice.DueDate, x.invoice.Status
            ));

        var paginatedResult = await PaginatedList<InvoiceSummaryDto>.CreateAsync(
            projectedQuery, request.PageNumber, request.PageSize, cancellationToken);

        return Result.Success(paginatedResult);
    }
}
