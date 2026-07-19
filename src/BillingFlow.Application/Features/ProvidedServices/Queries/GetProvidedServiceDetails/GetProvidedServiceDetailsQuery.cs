using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServiceDetails;

public record GetProvidedServiceDetailsQuery(Guid ServiceId) : IQuery<ProvidedServiceDetailsDto>, IRequirePolicy;
