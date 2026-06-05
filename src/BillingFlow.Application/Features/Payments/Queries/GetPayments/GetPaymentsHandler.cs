using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Payments.Queries.GetPayments;

public class GetPaymentsHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IRequestHandler<GetPaymentsQuery, PaginatedList<PaymentSummaryDto>>
{
    public async Task<PaginatedList<PaymentSummaryDto>> Handle(GetPaymentsQuery request, CancellationToken cancellationToken)
    {
        // 1. Base Query with JOIN for context and security
        var query = from payment in context.Payments.AsNoTracking()
                    join invoice in context.Invoices.AsNoTracking() on payment.InvoiceId equals invoice.Id
                    select new { payment, invoice };

        // 2. Row-Level Security (RLS) for Customers
        if (currentUserService.UserRole == Role.Customer)
        {
            query = query.Where(x => x.invoice.OwnerUserId == currentUserService.UserId);
        }

        // 3. Apply Filters
        if (request.FilterByClientId.HasValue)
            query = query.Where(x => x.invoice.ClientId == request.FilterByClientId.Value);

        if (request.FilterByInvoiceId.HasValue)
            query = query.Where(x => x.payment.InvoiceId == request.FilterByInvoiceId.Value);

        if (request.FilterByProvider.HasValue)
            query = query.Where(x => x.payment.Provider == request.FilterByProvider.Value);

        if (request.FilterByMethod.HasValue)
            query = query.Where(x => x.payment.Method == request.FilterByMethod.Value);

        // 4. Apply Search Term (e.g., searching by Invoice Number or External Transaction ID)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.Trim();
            query = query.Where(x =>
                x.invoice.InvoiceNumber.Contains(search) ||
                (x.payment.ExternalTransactionId != null && x.payment.ExternalTransactionId.Contains(search))
            );
        }

        // 5. Projection
        var projectedQuery = query
            .OrderByDescending(x => x.payment.PaymentDate)
            .Select(x => new PaymentSummaryDto(
                x.payment.Id,
                x.payment.InvoiceId,
                x.invoice.InvoiceNumber,
                x.payment.Amount,
                x.payment.PaymentDate,
                x.payment.Provider,
                x.payment.Method,
                x.payment.ExternalTransactionId
            ));

        // 6. Pagination Execution
        return await PaginatedList<PaymentSummaryDto>.CreateAsync(
            projectedQuery, request.PageNumber, request.PageSize, cancellationToken);
    }
}
