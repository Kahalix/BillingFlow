using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Clients.Commands.LinkClientUser;

public record LinkClientUserCommand(Guid ClientId, Guid UserId) : ICommand, IRequirePermission;
