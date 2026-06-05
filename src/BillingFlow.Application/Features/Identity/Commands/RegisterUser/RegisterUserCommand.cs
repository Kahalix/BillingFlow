using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Domain.Enums;

using MediatR;

public record RegisterUserCommand(
    string Email,
    string Password,
    Role TargetRole) : IRequest<Guid>, IAuthenticatedRequest, IRequirePermission, IRequirePolicy;
