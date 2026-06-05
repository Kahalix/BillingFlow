using System;
using System.Diagnostics;

using BillingFlow.Application.Interfaces;

using Microsoft.AspNetCore.Http;

namespace BillingFlow.Infrastructure.Identity;

public class AuditContext(
    ICurrentUserService currentUserService,
    IHttpContextAccessor httpContextAccessor) : IAuditContext
{
    public Guid? UserId => currentUserService.IsAuthenticated ? currentUserService.UserId : null;

    // Use standard W3C TraceId for distributed tracing correlation (OpenTelemetry).
    // Fallback to ASP.NET Core local TraceIdentifier if Activity is not present.
    public string? TraceId => Activity.Current?.TraceId.ToString() ?? httpContextAccessor.HttpContext?.TraceIdentifier;

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public string? UserAgent => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string? HttpMethod => httpContextAccessor.HttpContext?.Request.Method;

    public string? RequestPath => httpContextAccessor.HttpContext?.Request.Path.Value;
}
