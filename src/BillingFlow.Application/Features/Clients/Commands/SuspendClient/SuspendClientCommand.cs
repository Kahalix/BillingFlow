using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Clients.Commands.SuspendClient;

/// <summary>
/// Command to suspend a billing client, halting their financial pipeline.
/// </summary>
public record SuspendClientCommand(Guid ClientId) : ICommand, IRequirePermission;
