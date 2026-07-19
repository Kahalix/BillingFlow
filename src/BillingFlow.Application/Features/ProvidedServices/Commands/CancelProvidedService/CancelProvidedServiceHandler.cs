using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.CancelProvidedService;

public class CancelProvidedServiceHandler(IApplicationDbContext context)
    : ICommandHandler<CancelProvidedServiceCommand>
{
    public async Task<Result> Handle(CancelProvidedServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await context.ProvidedServices
            .SingleOrDefaultAsync(ps => ps.Id == request.ServiceId, cancellationToken);

        if (service == null)
            return Result.Failure(Errors.ProvidedServices.NotFound(request.ServiceId));

        // Domain method mutates state to Canceled
        service.Cancel();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
