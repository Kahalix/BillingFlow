using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Clients.Commands.UnlinkClientUser;

public record UnlinkClientUserCommand(Guid ClientId) : ICommand, IRequirePermission;
