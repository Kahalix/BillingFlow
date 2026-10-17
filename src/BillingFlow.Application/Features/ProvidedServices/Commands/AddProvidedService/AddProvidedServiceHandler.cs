using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.AddProvidedService;

public class AddProvidedServiceHandler(
    IApplicationDbContext context,
    TimeProvider timeProvider) : ICommandHandler<AddProvidedServiceCommand, Guid>
{
    public async Task<Result<Guid>> Handle(AddProvidedServiceCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Client exists and is capable of receiving new billable services
        // IGNORE QUERY FILTERS: We want to explicitly catch Archived clients and throw Forbidden, not NotFound.
        var clientStatus = await context.Clients
            .IgnoreQueryFilters()
            .Where(c => c.Id == request.ClientId)
            .Select(c => (ClientStatus?)c.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (clientStatus is null)
        {
            return Result.Failure<Guid>(Errors.Clients.NotFound(request.ClientId));
        }

        if (clientStatus != ClientStatus.Active)
        {
            return Result.Failure<Guid>(Errors.ProvidedServices.ClientNotActive);
        }

        // 2. Delegate to Domain Factory
        var now = timeProvider.GetUtcNow();
        var service = ProvidedService.Create(
            request.ClientId,
            request.Description,
            request.Amount,
            request.PerformedAt,
            now
        );

        // 3. Persist state
        context.ProvidedServices.Add(service);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(service.Id);
    }
}
