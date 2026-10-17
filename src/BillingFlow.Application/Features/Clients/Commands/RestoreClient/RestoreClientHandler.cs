using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.RestoreClient;

public class RestoreClientHandler(IApplicationDbContext context) : ICommandHandler<RestoreClientCommand>
{
    public async Task<Result> Handle(RestoreClientCommand request, CancellationToken cancellationToken)
    {
        // IgnoreQueryFilters is MANDATORY here, otherwise the archived client will return null (404)
        var client = await context.Clients
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            return Result.Failure(Errors.Clients.NotFound(request.ClientId));
        }

        // Delegate to domain logic
        client.Restore();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
