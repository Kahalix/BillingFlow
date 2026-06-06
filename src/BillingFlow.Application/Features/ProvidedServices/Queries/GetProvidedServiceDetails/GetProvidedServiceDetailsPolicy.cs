using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServiceDetails;

public class GetProvidedServiceDetailsPolicy(
    IApplicationDbContext context,
    ICurrentUserService currentUserService) : IAuthorizationPolicy<GetProvidedServiceDetailsQuery>
{
    public async Task<bool> AuthorizeAsync(GetProvidedServiceDetailsQuery request, CancellationToken cancellationToken)
    {
        var role = currentUserService.UserRole;
        var userId = currentUserService.UserId;

        // Back-office roles bypass the explicit ownership check
        if (role is Role.Admin or Role.Manager or Role.Accountant or Role.Employee)
        {
            return true;
        }

        // Fetch the ownership chain: ProvidedService -> Client -> UserId
        var ownershipData = await context.ProvidedServices.AsNoTracking()
            .Where(ps => ps.Id == request.ServiceId)
            .Join(
                context.Clients.IgnoreQueryFilters(),
                service => service.ClientId,
                client => client.Id,
                (service, client) => new { client.UserId }
            )
            .SingleOrDefaultAsync(cancellationToken);

        // Allow the handler to throw a 404 NotFound instead of 403 Forbidden if the resource doesn't exist
        if (ownershipData is null)
        {
            return true;
        }

        return ownershipData.UserId == userId;
    }
}
