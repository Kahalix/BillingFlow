// File: src/BillingFlow.Application/Features/Invoices/Queries/DownloadInvoicePdf/InvoicePdfDto.cs
namespace BillingFlow.Application.Features.Invoices.Queries.DownloadInvoicePdf;

/// <summary>
/// Data Transfer Object carrying the binary payload and metadata for the rendered invoice.
/// </summary>
public record InvoicePdfDto(
    byte[] Content,
    string FileName
);
