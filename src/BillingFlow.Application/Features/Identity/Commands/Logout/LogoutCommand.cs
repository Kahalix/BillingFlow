// File: src/BillingFlow.Application/Features/Identity/Commands/Logout/LogoutCommand.cs
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.Logout;

/// <summary>
/// Revokes the current active session.
/// Requires an authenticated context to extract the SessionId.
/// </summary>
public record LogoutCommand : IRequest, IAuthenticatedRequest;
