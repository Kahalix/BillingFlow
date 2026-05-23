// File: src/BillingFlow.Application/Features/Clients/Commands/UnlinkClientUser/UnlinkClientUserCommand.cs
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Commands.UnlinkClientUser;

public record UnlinkClientUserCommand(Guid ClientId) : IRequest, IRequirePermission;
