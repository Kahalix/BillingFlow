using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Invoices.Commands.GenerateInvoice;

public class GenerateInvoiceHandler(
    IApplicationDbContext context,
    IInvoiceNumberGenerator numberGenerator,
    TimeProvider timeProvider) : ICommandHandler<GenerateInvoiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(GenerateInvoiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch Client status AND UserId enforcing soft-delete awareness
        var clientInfo = await context.Clients
            .IgnoreQueryFilters()
            .Where(c => c.Id == request.ClientId)
            .Select(c => new { c.Status, c.UserId })
            .SingleOrDefaultAsync(cancellationToken);

        if (clientInfo is null)
        {
            return Result.Failure<Guid>(Errors.Clients.NotFound(request.ClientId));
        }

        // Business Rule: Suspended or Archived clients cannot have invoices processed
        if (clientInfo.Status != ClientStatus.Active)
        {
            return Result.Failure<Guid>(Errors.Invoices.ClientNotActive);
        }

        // 2. Query all un-billed provided services within the given time matrix
        var unbilledServices = await context.ProvidedServices
            .Where(ps => ps.ClientId == request.ClientId &&
                         ps.InvoiceId == null &&
                         ps.PerformedAt >= request.FromDate &&
                         ps.PerformedAt <= request.ToDate)
            .ToListAsync(cancellationToken);

        if (!unbilledServices.Any())
        {
            return Result.Failure<Guid>(Errors.Invoices.NoUnbilledServices);
        }

        // 3. Obtain a legal sequence invoice number
        var invoiceNumber = await numberGenerator.GenerateNextNumberAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();

        // 4. Initialize the Invoice Aggregate Root in Draft state, preserving OwnerUserId snapshot
        var invoice = Invoice.Create(request.ClientId, clientInfo.UserId, invoiceNumber, now, paymentTermsDays: 14);

        // 5. Transfer service items to the invoice and mark source data as resolved
        foreach (var service in unbilledServices)
        {
            // Encapsulated calculation happens safely inside the Aggregate Root
            invoice.AddLineItem(service.Description, service.Amount, quantity: 1);

            // Mutate child state to prevent double-billing loops
            service.MarkAsBilled(invoice.Id, now);
        }

        // 6. Transition status from Draft to Unpaid and seal the aggregate (triggers InvoiceGeneratedEvent)
        invoice.Issue();

        // 7. Commit transaction atomicity
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(invoice.Id);
    }
}
