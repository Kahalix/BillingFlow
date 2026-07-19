using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Features.Invoices.Common.Models;

namespace BillingFlow.Application.Features.Invoices.Common;

public interface IInvoiceDataProvider
{
    Task<Result<InvoiceDetailsModel>> GetInvoiceDetailsAsync(Guid invoiceId, CancellationToken cancellationToken);
}
