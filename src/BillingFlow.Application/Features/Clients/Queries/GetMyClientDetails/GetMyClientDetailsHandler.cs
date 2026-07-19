using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Features.Clients.Queries.GetClientDetails;
using BillingFlow.Application.Features.Clients.Queries.Projections;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Queries.GetMyClientDetails;

public class GetMyClientDetailsHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetMyClientDetailsQuery, ClientDetailsResponse>
{
    public async Task<Result<ClientDetailsResponse>> Handle(GetMyClientDetailsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;

        var client = await context.Clients
            .AsNoTracking()
            .Where(c => c.UserId == currentUserId)
            .Select(ClientProjections.Details)
            .SingleOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            return Result.Failure<ClientDetailsResponse>(Errors.Clients.ProfileNotFound);
        }

        return Result.Success(client);
    }
}
