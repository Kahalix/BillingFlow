// File: src/BillingFlow.Application/Features/Clients/Commands/RestoreClient/RestoreClientCommand.cs
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Commands.RestoreClient;

public record RestoreClientCommand(Guid ClientId) : IRequest, IRequirePermission;
