using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.AddProvidedService;

public record AddProvidedServiceCommand(
    Guid ClientId,
    string Description,
    decimal Amount,
    DateTimeOffset PerformedAt
) : IRequest<Guid>, IRequirePermission;
