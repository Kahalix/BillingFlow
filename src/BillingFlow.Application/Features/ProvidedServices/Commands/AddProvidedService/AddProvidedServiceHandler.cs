using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.AddProvidedService;

public class AddProvidedServiceHandler(
    IApplicationDbContext context,
    TimeProvider timeProvider) : IRequestHandler<AddProvidedServiceCommand, Guid>
{
    public async Task<Guid> Handle(AddProvidedServiceCommand request, CancellationToken cancellationToken)
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
            throw new NotFoundException("Client profile could not be found.");
        }

        if (clientStatus != ClientStatus.Active)
        {
            throw new ForbiddenException("Cannot add billable services to a suspended or archived client.");
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

        return service.Id;
    }
}
