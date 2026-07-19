using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.UpdateClient;

public class UpdateClientHandler(IApplicationDbContext context) : ICommandHandler<UpdateClientCommand>
{
    public async Task<Result> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            return Result.Failure(Errors.Clients.NotFound(request.ClientId));
        }

        var newAddress = new Address(
            request.Street,
            request.City,
            request.PostalCode,
            request.Country
        );

        client.UpdateDetails(request.CompanyName, request.TaxId, newAddress);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
