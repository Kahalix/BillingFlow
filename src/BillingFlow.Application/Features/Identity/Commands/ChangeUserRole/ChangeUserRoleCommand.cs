// File: src/BillingFlow.Application/Features/Identity/Commands/ChangeUserRole/ChangeUserRoleCommand.cs
using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Domain.Enums;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, Role NewRole) : IRequest, IRequirePermission, IRequirePolicy
{
    public string Permission => AppPermissions.UsersChangeRole;
}
