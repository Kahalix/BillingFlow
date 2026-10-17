using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.ArchiveClient;

public class ArchiveClientHandler(IApplicationDbContext context) : ICommandHandler<ArchiveClientCommand>
{
    public async Task<Result> Handle(ArchiveClientCommand request, CancellationToken cancellationToken)
    {
        // We use IgnoreQueryFilters() in case we need to process an already archived client 
        // without throwing a misleading 404 NotFound error.
        var client = await context.Clients
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            return Result.Failure(Errors.Clients.NotFound(request.ClientId));
        }

        // Domain handles state transition and registers ClientArchivedEvent
        client.Archive();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
