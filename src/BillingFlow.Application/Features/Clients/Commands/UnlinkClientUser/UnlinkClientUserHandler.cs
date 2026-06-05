using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.UnlinkClientUser;

public class UnlinkClientUserHandler(IApplicationDbContext context) : IRequestHandler<UnlinkClientUserCommand>
{
    public async Task Handle(UnlinkClientUserCommand request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Client profile could not be found.");
        }

        // Domain method validates state and removes the linkage
        client.UnlinkUser();

        await context.SaveChangesAsync(cancellationToken);
    }
}
