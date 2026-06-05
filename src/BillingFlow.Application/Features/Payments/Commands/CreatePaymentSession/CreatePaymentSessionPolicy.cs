// File: src/BillingFlow.Application/Features/Payments/Commands/CreatePaymentSession/CreatePaymentSessionPolicy.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;

public class CreatePaymentSessionPolicy(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IAuthorizationPolicy<CreatePaymentSessionCommand>
{
    public async Task<bool> AuthorizeAsync(CreatePaymentSessionCommand request, CancellationToken cancellationToken)
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
            return true; // Let the handler throw 404

        return invoice.OwnerUserId == userId;
    }
}
