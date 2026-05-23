// File: src/BillingFlow.Application/Features/Clients/Commands/SuspendClient/SuspendClientCommand.cs
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Commands.SuspendClient;

/// <summary>
/// Command to suspend a billing client, halting their financial pipeline.
/// </summary>
public record SuspendClientCommand(Guid ClientId) : IRequest, IRequirePermission;
