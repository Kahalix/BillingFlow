// File: src/BillingFlow.Application/Features/Clients/Commands/ActivateClient/ActivateClientCommand.cs
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Commands.ActivateClient;

public record ActivateClientCommand(Guid ClientId) : IRequest, IRequirePermission;
