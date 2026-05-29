// File: src/BillingFlow.Application/Interfaces/IInvoicePdfRenderer.cs
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Invoices.Common.Models;

namespace BillingFlow.Application.Interfaces;

public interface IInvoicePdfRenderer
{
    Task<byte[]> RenderAsync(InvoiceDetailsModel invoiceData, CancellationToken cancellationToken);
}
