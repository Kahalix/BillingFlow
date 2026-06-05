using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Exceptions;
using BillingFlow.Domain.ValueObjects;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.CreateClient;

public class CreateClientHandler(IApplicationDbContext context)
    : IRequestHandler<CreateClientCommand, CreateClientResponse>
{
    public async Task<CreateClientResponse> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        // 1. Enforce 1:1 relationship invariant (One AppUser = One Client Profile)
        if (request.UserId.HasValue)
        {
            var userExists = await context.Users.AnyAsync(u => u.Id == request.UserId.Value, cancellationToken);
            if (!userExists)
            {
                throw new NotFoundException("The specified User ID does not exist.");
            }

            var clientAlreadyLinked = await context.Clients
                .AnyAsync(c => c.UserId == request.UserId.Value, cancellationToken);

            if (clientAlreadyLinked)
            {
                throw new DomainException("This user is already linked to another client profile.");
            }
        }

        // 2. Map structural data to Domain Value Object (encapsulates formatting validation)
        var address = new Address(
            request.Street,
            request.City,
            request.PostalCode,
            request.Country
        );

        // 3. Instantiate Aggregate Root via Domain Factory Method
        var client = Client.Create(
            request.UserId,
            request.CompanyName,
            request.TaxId,
            address
        );

        // 4. Persist to write database
        context.Clients.Add(client);
        await context.SaveChangesAsync(cancellationToken);

        return new CreateClientResponse(client.Id);
    }
}
