// File: src/BillingFlow.Application/Features/Invoices/Queries/GetInvoiceDetails/GetInvoiceDetailsHandler.cs
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Invoices.Common;
using BillingFlow.Application.Features.Invoices.Common.Models;

using MediatR;

namespace BillingFlow.Application.Features.Invoices.Queries.GetInvoiceDetails;

public class GetInvoiceDetailsHandler(IInvoiceDataProvider dataProvider)
    : IRequestHandler<GetInvoiceDetailsQuery, InvoiceDetailsModel>
{
    public async Task<InvoiceDetailsModel> Handle(GetInvoiceDetailsQuery request, CancellationToken cancellationToken)
    {
        // One unified source of truth for querying invoice data
        return await dataProvider.GetInvoiceDetailsAsync(request.InvoiceId, cancellationToken);
    }
}
