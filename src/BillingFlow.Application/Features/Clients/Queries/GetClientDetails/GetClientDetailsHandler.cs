using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Clients.Queries.Projections;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientDetails;

public class GetClientDetailsHandler(IApplicationDbContext context)
    : IRequestHandler<GetClientDetailsQuery, ClientDetailsResponse>
{
    public async Task<ClientDetailsResponse> Handle(GetClientDetailsQuery request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .AsNoTracking()
            .Where(c => c.Id == request.ClientId)
            .Select(ClientProjections.Details)
            .SingleOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Client profile could not be found.");
        }

        return client;
    }
}
