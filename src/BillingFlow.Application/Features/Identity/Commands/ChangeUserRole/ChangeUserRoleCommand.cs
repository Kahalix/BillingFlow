using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Domain.Enums;

using System;

namespace BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, Role NewRole) : ICommand, IRequirePermission, IRequirePolicy;
