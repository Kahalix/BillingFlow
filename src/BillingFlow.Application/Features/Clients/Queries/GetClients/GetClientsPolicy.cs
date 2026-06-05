using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Clients.Queries.GetClients;

/// <summary>
/// Evaluates dynamic access rules for the client list.
/// Enforces that only Admins and Managers can query for archived client profiles.
/// </summary>
public class GetClientsPolicy(ICurrentUserService currentUserService) : IAuthorizationPolicy<GetClientsQuery>
{
    public Task<bool> AuthorizeAsync(GetClientsQuery request, CancellationToken cancellationToken)
    {
        // 1. If the user is not specifically asking for archived records, allow the query.
        // Base permission (ClientsRead) is already enforced at the Controller level.
        if (request.Status != ClientStatus.Archived)
        {
            return Task.FromResult(true);
        }

        // 2. If requesting Archived records, verify hierarchical role access.
        var role = currentUserService.UserRole;
        var isAllowedToViewArchives = role is Role.Admin or Role.Manager;

        return Task.FromResult(isAllowedToViewArchives);
    }
}
