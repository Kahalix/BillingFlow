using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Invoices.Queries.DownloadInvoicePdf;

/// <summary>
/// Triggers the generation and retrieval of an invoice PDF document.
/// Protected by the associated DownloadInvoicePdfPolicy.
/// </summary>
public record DownloadInvoicePdfQuery(Guid InvoiceId) : IRequest<InvoicePdfDto>, IRequirePolicy;
