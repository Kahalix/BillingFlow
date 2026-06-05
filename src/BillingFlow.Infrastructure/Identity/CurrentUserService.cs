using System.Security.Claims;

using BillingFlow.Application.Authorization;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.AspNetCore.Http;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Retrieves current user information from the ASP.NET Core HttpContext.
/// </summary>
public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;

    public Role UserRole =>
        Enum.TryParse<Role>(User?.FindFirst(ClaimTypes.Role)?.Value, out var role) ? role : default;

    public Guid SessionId =>
        Guid.TryParse(User?.FindFirst(CustomClaimTypes.SessionId)?.Value, out var id) ? id : Guid.Empty;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
