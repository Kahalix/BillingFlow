using System;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Identity.Commands.DashboardLogin;

/// <summary>
/// Returns only the precise claims required to construct an isolated browser cookie.
/// Explicitly avoids returning JWTs to prevent mixed-auth smells.
/// </summary>
public record DashboardLoginResponse(Guid UserId, Role Role, Guid SessionId);
