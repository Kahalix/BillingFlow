using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.SuspendUser;

public record SuspendUserCommand(Guid UserId) : IRequest, IRequirePermission, IRequirePolicy;
