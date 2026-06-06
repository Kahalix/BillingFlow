using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.CancelProvidedService;

public record CancelProvidedServiceCommand(Guid ServiceId) : IRequest, IRequirePermission;
