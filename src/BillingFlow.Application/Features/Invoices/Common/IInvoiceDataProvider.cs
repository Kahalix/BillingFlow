// File: src/BillingFlow.Application/Features/Invoices/Common/IInvoiceDataProvider.cs
using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Invoices.Common.Models;

namespace BillingFlow.Application.Features.Invoices.Common;

public interface IInvoiceDataProvider
{
    Task<InvoiceDetailsModel> GetInvoiceDetailsAsync(Guid invoiceId, CancellationToken cancellationToken);
}
