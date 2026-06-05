// File: src/BillingFlow.Application/Features/Payments/Queries/GetPaymentDetails/GetPaymentDetailsHandler.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Payments.Queries.GetPaymentDetails;

public class GetPaymentDetailsHandler(IApplicationDbContext context)
    : IRequestHandler<GetPaymentDetailsQuery, PaymentDetailsResponse>
{
    public async Task<PaymentDetailsResponse> Handle(GetPaymentDetailsQuery request, CancellationToken cancellationToken)
    {
        var response = await (from p in context.Payments.AsNoTracking()
                              join i in context.Invoices.AsNoTracking() on p.InvoiceId equals i.Id
                              where p.Id == request.Id
                              select new PaymentDetailsResponse(
                                  p.Id,
                                  p.InvoiceId,
                                  i.InvoiceNumber,
                                  p.Amount,
                                  p.PaymentDate,
                                  p.Provider,
                                  p.Method,
                                  p.ExternalTransactionId,
                                  p.ReceivedByUserId,
                                  p.Notes,
                                  p.CreatedAt
                              )).SingleOrDefaultAsync(cancellationToken);

        if (response == null)
            throw new NotFoundException($"Payment with ID {request.Id} could not be found.");

        return response;
    }
}
