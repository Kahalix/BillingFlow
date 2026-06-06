using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.UpdateProvidedService;

public record UpdateProvidedServiceCommand(
    Guid ServiceId,
    string Description,
    decimal Amount,
    DateTimeOffset PerformedAt
) : IRequest, IRequirePermission;
