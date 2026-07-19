using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.LinkClientUser;

public class LinkClientUserHandler(IApplicationDbContext context) : ICommandHandler<LinkClientUserCommand>
{
    public async Task<Result> Handle(LinkClientUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify target user exists
        var userExists = await context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
        if (!userExists)
        {
            return Result.Failure(Errors.Users.NotFound(request.UserId));
        }

        // 2. Enforce global invariant: Target user cannot already be linked to another profile
        var userAlreadyLinked = await context.Clients.AnyAsync(c => c.UserId == request.UserId, cancellationToken);
        if (userAlreadyLinked)
        {
            return Result.Failure(Errors.Clients.UserAlreadyLinked);
        }

        // 3. Fetch target client profile
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            return Result.Failure(Errors.Clients.NotFound(request.ClientId));
        }

        // 4. Delegate to domain logic
        client.LinkUser(request.UserId);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
