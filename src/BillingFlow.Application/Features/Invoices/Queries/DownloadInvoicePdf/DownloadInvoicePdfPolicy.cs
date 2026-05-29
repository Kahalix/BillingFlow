// File: src/BillingFlow.Application/Features/Invoices/Queries/DownloadInvoicePdf/DownloadInvoicePdfPolicy.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Invoices.Queries.DownloadInvoicePdf;

public class DownloadInvoicePdfPolicy(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IAuthorizationPolicy<DownloadInvoicePdfQuery>
{
    public async Task<bool> AuthorizeAsync(DownloadInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        var role = currentUserService.UserRole;
        var userId = currentUserService.UserId;

        // Back-office employees bypass the ownership check
        if (role is Role.Admin or Role.Manager or Role.Accountant)
            return true;

        var invoice = await context.Invoices.AsNoTracking()
            .Where(i => i.Id == request.InvoiceId)
            .Select(i => new { i.OwnerUserId })
            .SingleOrDefaultAsync(cancellationToken);

        if (invoice is null)
            return true; // Let the Handler throw 404 NotFound

        // ABAC: Access is granted ONLY if the user ID matches the historical snapshot.
        return invoice.OwnerUserId == userId;
    }
}
