// File: src/BillingFlow.Application/Features/Clients/Queries/GetMyClientDetails/GetMyClientDetailsHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Clients.Queries.GetClientDetails;
using BillingFlow.Application.Features.Clients.Queries.Projections;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Queries.GetMyClientDetails;

public class GetMyClientDetailsHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetMyClientDetailsQuery, ClientDetailsResponse>
{
    public async Task<ClientDetailsResponse> Handle(GetMyClientDetailsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;

        var client = await context.Clients
            .AsNoTracking()
            .Where(c => c.UserId == currentUserId)
            .Select(ClientProjections.Details)
            .SingleOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("You do not have an associated client profile.");
        }

        return client;
    }
}
