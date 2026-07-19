using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

using System;

namespace BillingFlow.Application.Features.Identity.Commands.ActivateUser;

public record ActivateUserCommand(Guid UserId) : ICommand, IRequirePermission, IRequirePolicy;
