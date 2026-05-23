// File: src/BillingFlow.Application/Features/Clients/Commands/ActivateClient/ActivateClientHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.ActivateClient;

public class ActivateClientHandler(IApplicationDbContext context) : IRequestHandler<ActivateClientCommand>
{
    public async Task Handle(ActivateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Client profile could not be found.");
        }

        client.Activate();

        await context.SaveChangesAsync(cancellationToken);
    }
}
