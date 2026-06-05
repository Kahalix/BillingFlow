using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.ValueObjects;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.UpdateClient;

public class UpdateClientHandler(IApplicationDbContext context) : IRequestHandler<UpdateClientCommand>
{
    public async Task Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await context.Clients
            .SingleOrDefaultAsync(c => c.Id == request.ClientId, cancellationToken);

        if (client is null)
        {
            throw new NotFoundException("Client profile could not be found.");
        }

        var newAddress = new Address(
            request.Street,
            request.City,
            request.PostalCode,
            request.Country
        );

        // Zlecamy domenowe sprawdzenie reguł i aktualizację
        client.UpdateDetails(request.CompanyName, request.TaxId, newAddress);

        await context.SaveChangesAsync(cancellationToken);
    }
}
