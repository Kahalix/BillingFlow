using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServiceDetails;

public class GetProvidedServiceDetailsHandler(IApplicationDbContext context)
    : IQueryHandler<GetProvidedServiceDetailsQuery, ProvidedServiceDetailsDto>
{
    public async Task<Result<ProvidedServiceDetailsDto>> Handle(GetProvidedServiceDetailsQuery request, CancellationToken cancellationToken)
    {
        var serviceDetails = await context.ProvidedServices
            .AsNoTracking()
            .Where(ps => ps.Id == request.ServiceId)
            .Join(
                context.Clients.IgnoreQueryFilters(),
                service => service.ClientId,
                client => client.Id,
                (service, client) => new ProvidedServiceDetailsDto(
                    service.Id,
                    service.ClientId,
                    client.CompanyName,
                    service.Description,
                    service.Amount,
                    service.PerformedAt,
                    service.InvoiceId,
                    service.Status
                )
            )
            .SingleOrDefaultAsync(cancellationToken);

        if (serviceDetails is null)
        {
            return Result.Failure<ProvidedServiceDetailsDto>(Errors.ProvidedServices.NotFound(request.ServiceId));
        }

        return Result.Success(serviceDetails);
    }
}
