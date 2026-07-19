using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.AddProvidedService;

public record AddProvidedServiceCommand(
    Guid ClientId,
    string Description,
    decimal Amount,
    DateTimeOffset PerformedAt
) : ICommand<Guid>, IRequirePermission;
