using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.ActivateClient;

public class ActivateClientHandler(IApplicationDbContext context) : ICommandHandler<ActivateClientCommand>
{
    public async Task<Result> Handle(ActivateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            return Result.Failure(Errors.Clients.NotFound(request.ClientId));
        }

        client.Activate();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
