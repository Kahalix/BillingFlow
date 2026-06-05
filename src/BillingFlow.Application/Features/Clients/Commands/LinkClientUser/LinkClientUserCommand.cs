using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Commands.LinkClientUser;

public record LinkClientUserCommand(Guid ClientId, Guid UserId) : IRequest, IRequirePermission;
