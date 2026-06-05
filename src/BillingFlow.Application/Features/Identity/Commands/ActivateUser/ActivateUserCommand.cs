using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.ActivateUser;

public record ActivateUserCommand(Guid UserId) : IRequest, IRequirePermission, IRequirePolicy;
