using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServiceDetails;

public record GetProvidedServiceDetailsQuery(Guid ServiceId) : IRequest<ProvidedServiceDetailsDto>, IRequirePolicy;
