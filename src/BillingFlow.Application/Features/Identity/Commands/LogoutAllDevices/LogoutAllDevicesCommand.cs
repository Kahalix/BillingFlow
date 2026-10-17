using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.LogoutAllDevices;

/// <summary>
/// Revokes all active sessions across all devices for the currently authenticated user.
/// </summary>
public record LogoutAllDevicesCommand : ICommand, IAuthenticatedRequest;
