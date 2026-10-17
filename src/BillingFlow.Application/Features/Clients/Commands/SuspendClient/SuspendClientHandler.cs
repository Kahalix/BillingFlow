using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.SuspendClient;

public class SuspendClientHandler(IApplicationDbContext context) : ICommandHandler<SuspendClientCommand>
{
    public async Task<Result> Handle(SuspendClientCommand request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            return Result.Failure(Errors.Clients.NotFound(request.ClientId));
        }

        // The domain entity encapsulates the logic. 
        // Calling this method mutates the status and registers a ClientSuspendedEvent internally.
        client.Suspend();

        // The EF Core DispatchDomainEventsInterceptor will catch the internal event 
        // and publish it via MediatR before committing the transaction.
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
