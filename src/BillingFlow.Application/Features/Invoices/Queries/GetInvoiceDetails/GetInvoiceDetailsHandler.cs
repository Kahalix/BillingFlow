using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Features.Invoices.Common;
using BillingFlow.Application.Features.Invoices.Common.Models;

namespace BillingFlow.Application.Features.Invoices.Queries.GetInvoiceDetails;

public class GetInvoiceDetailsHandler(IInvoiceDataProvider dataProvider)
    : IQueryHandler<GetInvoiceDetailsQuery, InvoiceDetailsModel>
{
    public async Task<Result<InvoiceDetailsModel>> Handle(GetInvoiceDetailsQuery request, CancellationToken cancellationToken)
    {
        // One unified source of truth for querying invoice data.
        // It already returns a Result<T>, so we can simply return it directly.
        return await dataProvider.GetInvoiceDetailsAsync(request.InvoiceId, cancellationToken);
    }
}
