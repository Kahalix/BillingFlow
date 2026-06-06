using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.UpdateProvidedService;

public class UpdateProvidedServiceHandler(
    IApplicationDbContext context,
    TimeProvider timeProvider) : IRequestHandler<UpdateProvidedServiceCommand>
{
    public async Task Handle(UpdateProvidedServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await context.ProvidedServices
            .SingleOrDefaultAsync(ps => ps.Id == request.ServiceId, cancellationToken);

        if (service == null)
            throw new NotFoundException("Provided service not found.");

        // Aggregate Root enforces its own business rules (e.g., throwing if InvoiceId is not null)
        var now = timeProvider.GetUtcNow();
        service.UpdateDetails(request.Description, request.Amount, request.PerformedAt, now);

        await context.SaveChangesAsync(cancellationToken);
    }
}
