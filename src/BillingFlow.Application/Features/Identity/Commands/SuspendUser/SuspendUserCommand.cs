// File: src/BillingFlow.Application/Features/Identity/Commands/SuspendUser/SuspendUserCommand.cs
using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.SuspendUser;

public record SuspendUserCommand(Guid UserId) : IRequest, IRequirePermission, IRequirePolicy
{
    public string Permission => AppPermissions.UsersSuspend;
}
