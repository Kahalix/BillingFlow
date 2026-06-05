// File: src/BillingFlow.Application/Features/Invoices/Queries/GetInvoiceDetails/GetInvoiceDetailsPolicy.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Invoices.Queries.GetInvoiceDetails;

public class GetInvoiceDetailsPolicy(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IAuthorizationPolicy<GetInvoiceDetailsQuery>
{
    public async Task<bool> AuthorizeAsync(GetInvoiceDetailsQuery request, CancellationToken cancellationToken)
    {
        var role = currentUserService.UserRole;
        var userId = currentUserService.UserId;

        // Back-office employees bypass the ownership check
        if (role is Role.Admin or Role.Manager or Role.Accountant or Role.Employee)
        {
            return true;
        }

        // Fetch only the projection we need.
        var invoice = await context.Invoices.AsNoTracking()
            .Where(i => i.Id == request.InvoiceId)
            .Select(i => new { i.OwnerUserId })
            .SingleOrDefaultAsync(cancellationToken);

        // If the invoice doesn't exist, let the policy pass so the Handler throws a clean 404 NotFound
        if (invoice is null)
        {
            return true;
        }

        // ABAC: Access is granted ONLY if the user ID matches the historical snapshot.
        // If OwnerUserId is null (e.g. system-generated), it naturally rejects the customer.
        return invoice.OwnerUserId == userId;
    }
}
