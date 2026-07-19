using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.CancelProvidedService;

public record CancelProvidedServiceCommand(Guid ServiceId) : ICommand, IRequirePermission;
