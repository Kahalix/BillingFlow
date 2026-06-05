using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Payments.Queries.GetPaymentDetails;

public class GetPaymentDetailsPolicy(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IAuthorizationPolicy<GetPaymentDetailsQuery>
{
    public async Task<bool> AuthorizeAsync(GetPaymentDetailsQuery request, CancellationToken cancellationToken)
    {
        var role = currentUserService.UserRole;
        var userId = currentUserService.UserId;

        // Employees are granted global read access here so they can provide customer support 
        if (role is Role.Admin or Role.Manager or Role.Accountant or Role.Employee)
            return true;

        // For Customers, we enforce strict Row-Level Security (RLS)
        var ownerId = await (from p in context.Payments.AsNoTracking()
                             join i in context.Invoices.AsNoTracking() on p.InvoiceId equals i.Id
                             where p.Id == request.Id
                             select i.OwnerUserId)
                             .SingleOrDefaultAsync(cancellationToken);

        if (ownerId == null)
            return true;

        return ownerId == userId;
    }
}
