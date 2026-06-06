using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.CancelProvidedService;

public class CancelProvidedServiceHandler(IApplicationDbContext context)
    : IRequestHandler<CancelProvidedServiceCommand>
{
    public async Task Handle(CancelProvidedServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await context.ProvidedServices
            .SingleOrDefaultAsync(ps => ps.Id == request.ServiceId, cancellationToken);

        if (service == null)
            throw new NotFoundException("Provided service not found.");

        // Domain method mutates state to Canceled
        service.Cancel();

        await context.SaveChangesAsync(cancellationToken);
    }
}
