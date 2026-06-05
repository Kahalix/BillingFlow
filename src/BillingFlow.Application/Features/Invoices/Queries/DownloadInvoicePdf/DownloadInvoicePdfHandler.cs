using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Invoices.Common;
using BillingFlow.Application.Interfaces;

using MediatR;

namespace BillingFlow.Application.Features.Invoices.Queries.DownloadInvoicePdf;

public class DownloadInvoicePdfHandler(
    IInvoiceDataProvider dataProvider,
    IInvoicePdfRenderer pdfRenderer) : IRequestHandler<DownloadInvoicePdfQuery, InvoicePdfDto>
{
    public async Task<InvoicePdfDto> Handle(DownloadInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        // 1. Get data directly (bypassing double MediatR pipeline execution)
        var invoiceDetails = await dataProvider.GetInvoiceDetailsAsync(request.InvoiceId, cancellationToken);

        // 2. Render PDF
        var pdfBytes = await pdfRenderer.RenderAsync(invoiceDetails, cancellationToken);

        // 3. Format filename safely
        var safeInvoiceNumber = invoiceDetails.InvoiceNumber.Replace("/", "_").Replace("\\", "_");
        var fileName = $"Invoice_{safeInvoiceNumber}.pdf";

        return new InvoicePdfDto(pdfBytes, fileName);
    }
}
