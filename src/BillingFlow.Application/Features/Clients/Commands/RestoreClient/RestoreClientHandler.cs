using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.RestoreClient;

public class RestoreClientHandler(IApplicationDbContext context) : IRequestHandler<RestoreClientCommand>
{
    public async Task Handle(RestoreClientCommand request, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters is MANDATORY here, otherwise the archived client will return null (404)
        var client = await context.Clients
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Archived client profile could not be found.");
        }

        // Delegate to domain logic
        client.Restore();

        await context.SaveChangesAsync(cancellationToken);
    }
}
