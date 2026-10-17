using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Domain.Enums;

using System;

namespace BillingFlow.Application.Features.Identity.Commands.RegisterUser;

public record RegisterUserCommand(
    string Email,
    string Password,
    Role TargetRole) : ICommand<Guid>, IRequirePermission, IRequirePolicy;
