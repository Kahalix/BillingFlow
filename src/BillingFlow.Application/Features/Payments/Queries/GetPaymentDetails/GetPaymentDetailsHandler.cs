using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Payments.Queries.GetPaymentDetails;

public class GetPaymentDetailsHandler(IApplicationDbContext context)
    : IQueryHandler<GetPaymentDetailsQuery, PaymentDetailsResponse>
{
    public async Task<Result<PaymentDetailsResponse>> Handle(GetPaymentDetailsQuery request, CancellationToken cancellationToken)
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
            return Result.Failure<PaymentDetailsResponse>(Errors.Payments.NotFound(request.Id));

        return Result.Success(response);
    }
}
