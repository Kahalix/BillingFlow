using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.UnlinkClientUser;

public class UnlinkClientUserHandler(IApplicationDbContext context) : ICommandHandler<UnlinkClientUserCommand>
{
    public async Task<Result> Handle(UnlinkClientUserCommand request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            return Result.Failure(Errors.Clients.NotFound(request.ClientId));
        }

        // Domain method validates state and removes the linkage
        client.UnlinkUser();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
