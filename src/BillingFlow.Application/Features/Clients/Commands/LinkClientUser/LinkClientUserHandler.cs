// File: src/BillingFlow.Application/Features/Clients/Commands/LinkClientUser/LinkClientUserHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Exceptions;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.LinkClientUser;

public class LinkClientUserHandler(IApplicationDbContext context) : IRequestHandler<LinkClientUserCommand>
{
    public async Task Handle(LinkClientUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify target user exists
        var userExists = await context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            throw new NotFoundException("The specified User ID does not exist.");
        }

        // 2. Enforce global invariant: Target user cannot already be linked to another profile
        var userAlreadyLinked = await context.Clients.AnyAsync(c => c.UserId == request.UserId, cancellationToken);
        if (userAlreadyLinked)
        {
            throw new DomainException("This user account is already linked to another client profile.");
        }

        // 3. Fetch target client profile
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Client profile could not be found.");
        }

        // 4. Delegate to domain logic
        client.LinkUser(request.UserId);

        await context.SaveChangesAsync(cancellationToken);
    }
}
