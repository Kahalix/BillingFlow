using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Features.Invoices.Common;
using BillingFlow.Application.Interfaces;

namespace BillingFlow.Application.Features.Invoices.Queries.DownloadInvoicePdf;

public class DownloadInvoicePdfHandler(
    IInvoiceDataProvider dataProvider,
    IInvoicePdfRenderer pdfRenderer) : IQueryHandler<DownloadInvoicePdfQuery, InvoicePdfDto>
{
    public async Task<Result<InvoicePdfDto>> Handle(DownloadInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        // 1. Get data directly from provider. If failed (e.g. NotFound), propagate the failure result.
        var invoiceResult = await dataProvider.GetInvoiceDetailsAsync(request.InvoiceId, cancellationToken);

        if (invoiceResult.IsFailure)
        {
            return Result.Failure<InvoicePdfDto>(invoiceResult.Error);
        }

        var invoiceDetails = invoiceResult.Value;

        // 2. Render PDF
        var pdfBytes = await pdfRenderer.RenderAsync(invoiceDetails, cancellationToken);

        // 3. Format filename safely
        var safeInvoiceNumber = invoiceDetails.InvoiceNumber.Replace("/", "_").Replace("\\", "_");
        var fileName = $"Invoice_{safeInvoiceNumber}.pdf";

        return Result.Success(new InvoicePdfDto(pdfBytes, fileName));
    }
}
