using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.SuspendClient;

public class SuspendClientHandler(IApplicationDbContext context) : IRequestHandler<SuspendClientCommand>
{
    public async Task Handle(SuspendClientCommand request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Client profile could not be found.");
        }

        // The domain entity encapsulates the logic. 
        // Calling this method mutates the status and registers a ClientSuspendedEvent internally.
        client.Suspend();

        // The EF Core DispatchDomainEventsInterceptor will catch the internal event 
        // and publish it via MediatR before committing the transaction.
        await context.SaveChangesAsync(cancellationToken);
    }
}
