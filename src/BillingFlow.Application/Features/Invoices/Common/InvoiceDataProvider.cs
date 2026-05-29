// File: src/BillingFlow.Application/Features/Invoices/Common/InvoiceDataProvider.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Invoices.Common.Models;
using BillingFlow.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Invoices.Common;

public class InvoiceDataProvider(IApplicationDbContext context) : IInvoiceDataProvider
{
    public async Task<InvoiceDetailsModel> GetInvoiceDetailsAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        // 1. Fetch Header & Client Details
        var header = await (from invoice in context.Invoices.AsNoTracking()
                            join client in context.Clients.IgnoreQueryFilters() on invoice.ClientId equals client.Id
                            where invoice.Id == invoiceId
                            select new
                            {
                                invoice.Id,
                                invoice.InvoiceNumber,
                                invoice.TotalAmount,
                                invoice.PaidAmount,
                                invoice.IssueDate,
                                invoice.DueDate,
                                invoice.Status,
                                ClientId = client.Id,
                                client.CompanyName,
                                client.TaxId
                            })
                            .SingleOrDefaultAsync(cancellationToken);

        if (header is null)
            throw new NotFoundException($"Invoice with ID {invoiceId} could not be found.");

        // 2. Fetch Line Items
        var items = await context.InvoiceItems.AsNoTracking()
            .Where(item => item.InvoiceId == invoiceId)
            .Select(item => new InvoiceItemModel(
                item.Description,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal
            ))
            .ToListAsync(cancellationToken);

        // 3. Compose Final Payload
        return new InvoiceDetailsModel(
            header.Id,
            header.InvoiceNumber,
            new InvoiceClientModel(header.ClientId, header.CompanyName, header.TaxId),
            header.TotalAmount,
            header.PaidAmount,
            header.IssueDate,
            header.DueDate,
            header.Status,
            items
        );
    }
}
