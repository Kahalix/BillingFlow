using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Clients.Commands.RestoreClient;

public record RestoreClientCommand(Guid ClientId) : ICommand, IRequirePermission;
