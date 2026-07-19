using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Invoices.Queries.DownloadInvoicePdf;

/// <summary>
/// Triggers the generation and retrieval of an invoice PDF document.
/// Protected by the associated DownloadInvoicePdfPolicy.
/// </summary>
public record DownloadInvoicePdfQuery(Guid InvoiceId) : IQuery<InvoicePdfDto>, IRequirePolicy;
