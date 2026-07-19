using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Invoices.Commands.CancelInvoice;

public class CancelInvoiceHandler(IApplicationDbContext context) : ICommandHandler<CancelInvoiceCommand>
{
    public async Task<Result> Handle(CancelInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await context.Invoices
            .SingleOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return Result.Failure(Errors.Invoices.NotFound(request.InvoiceId));
        }

        invoice.Cancel();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
