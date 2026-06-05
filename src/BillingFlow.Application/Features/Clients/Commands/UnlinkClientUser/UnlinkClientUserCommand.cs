using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Commands.UnlinkClientUser;

public record UnlinkClientUserCommand(Guid ClientId) : IRequest, IRequirePermission;
