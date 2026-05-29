// File: src/BillingFlow.Application/Features/Invoices/Commands/CancelInvoice/CancelInvoiceHandler.cs
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Invoices.Commands.CancelInvoice;

public class CancelInvoiceHandler(IApplicationDbContext context) : IRequestHandler<CancelInvoiceCommand>
{
    public async Task Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await context.Invoices
            .SingleOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
            throw new NotFoundException($"Invoice with ID {request.InvoiceId} could not be found.");

        invoice.Cancel();

        await context.SaveChangesAsync(cancellationToken);
    }
}
