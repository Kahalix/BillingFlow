using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Clients.Commands.CreateClient;

public class CreateClientHandler(IApplicationDbContext context)
    : ICommandHandler<CreateClientCommand, CreateClientResponse>
{
    public async Task<Result<CreateClientResponse>> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        // 1. Enforce 1:1 relationship invariant (One AppUser = One Client Profile)
        if (request.UserId.HasValue)
        {
            var userExists = await context.Users.AnyAsync(u => u.Id == request.UserId.Value, cancellationToken);
            if (!userExists)
            {
                return Result.Failure<CreateClientResponse>(Errors.Users.NotFound(request.UserId.Value));
            }

            var clientAlreadyLinked = await context.Clients
                .AnyAsync(c => c.UserId == request.UserId.Value, cancellationToken);

            if (clientAlreadyLinked)
            {
                return Result.Failure<CreateClientResponse>(Errors.Clients.UserAlreadyLinked);
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

        return Result.Success(new CreateClientResponse(client.Id));
    }
}
