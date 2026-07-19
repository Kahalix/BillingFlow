using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.UpdateProvidedService;

public class UpdateProvidedServiceHandler(
    IApplicationDbContext context,
    TimeProvider timeProvider) : ICommandHandler<UpdateProvidedServiceCommand>
{
    public async Task<Result> Handle(UpdateProvidedServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await context.ProvidedServices
            .SingleOrDefaultAsync(ps => ps.Id == request.ServiceId, cancellationToken);

        if (service == null)
            return Result.Failure(Errors.ProvidedServices.NotFound(request.ServiceId));

        // Aggregate Root enforces its own business rules (e.g., throwing if InvoiceId is not null)
        var now = timeProvider.GetUtcNow();
        service.UpdateDetails(request.Description, request.Amount, request.PerformedAt, now);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
