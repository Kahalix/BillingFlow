using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Clients.Commands.ActivateClient;

public record ActivateClientCommand(Guid ClientId) : ICommand, IRequirePermission;
