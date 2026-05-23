// File: src/BillingFlow.Application/Features/Clients/Commands/ArchiveClient/ArchiveClientHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.ArchiveClient;

public class ArchiveClientHandler(IApplicationDbContext context) : IRequestHandler<ArchiveClientCommand>
{
    public async Task Handle(ArchiveClientCommand request, CancellationToken cancellationToken)
    {
        // We use IgnoreQueryFilters() in case we need to process an already archived client 
        // without throwing a misleading 404 NotFound error.
        var client = await context.Clients
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Client profile could not be found.");
        }

        // Domain handles state transition and registers ClientArchivedEvent
        client.Archive();

        await context.SaveChangesAsync(cancellationToken);
    }
}
