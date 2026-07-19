using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Features.Clients.Queries.Projections;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientDetails;

public class GetClientDetailsHandler(IApplicationDbContext context)
    : IQueryHandler<GetClientDetailsQuery, ClientDetailsResponse>
{
    public async Task<Result<ClientDetailsResponse>> Handle(GetClientDetailsQuery request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .AsNoTracking()
            .Where(c => c.Id == request.ClientId)
            .Select(ClientProjections.Details)
            .SingleOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            return Result.Failure<ClientDetailsResponse>(Errors.Clients.NotFound(request.ClientId));
        }

        return Result.Success(client);
    }
}
