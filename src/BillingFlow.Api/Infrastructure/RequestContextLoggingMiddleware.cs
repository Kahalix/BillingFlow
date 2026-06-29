using System.Diagnostics;
using System.Security.Claims;

using Serilog;
using Serilog.Context;

namespace BillingFlow.Api.Infrastructure;

/// <summary>
/// Enriches Serilog's ambient context (LogContext) and HTTP completion logs (DiagnosticContext)
/// with request trace and identity data, such as TraceId and UserId.
/// </summary>
public sealed class RequestContextLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IDiagnosticContext diagnosticContext)
    {
        // Resolve correlation identifier (W3C TraceContext or Kestrel fallback)
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        // Resolve authenticated user identity (Fallback to 'Anonymous' if unauthenticated)
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous";

        // Append TraceId to response headers for client-side troubleshooting
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Trace-Id"))
            {
                context.Response.Headers.Append("X-Trace-Id", traceId);
            }
            return Task.CompletedTask;
        });

        // Enrich the HTTP completion log (UseSerilogRequestLogging)
        diagnosticContext.Set("TraceId", traceId);
        diagnosticContext.Set("UserId", userId);

        // Enrich the ambient context for deep inline logs (Controllers, Services, EF Core)
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("UserId", userId))
        {
            await next(context);
        }
    }
}
