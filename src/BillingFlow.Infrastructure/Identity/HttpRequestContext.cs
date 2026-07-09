using BillingFlow.Application.Interfaces;

using Microsoft.AspNetCore.Http;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Retrieves HTTP-specific context. Registered as Scoped to capture ambient request state.
/// </summary>
public class HttpRequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext
{
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
