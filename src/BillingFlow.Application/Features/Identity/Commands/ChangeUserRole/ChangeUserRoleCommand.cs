using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Domain.Enums;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;

public record ChangeUserRoleCommand(Guid UserId, Role NewRole) : IRequest, IRequirePermission, IRequirePolicy;
