using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.LogoutAllDevices;

/// <summary>
/// Revokes all active sessions across all devices for the currently authenticated user.
/// </summary>
public record LogoutAllDevicesCommand : IRequest, IAuthenticatedRequest;
