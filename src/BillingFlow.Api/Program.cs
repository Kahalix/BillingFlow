using System.Net;

using BillingFlow.Api.Extensions;
using BillingFlow.Api.Hubs;
using BillingFlow.Api.Infrastructure;
using BillingFlow.Infrastructure.BackgroundJobs;

using Hangfire;

using Serilog;
using Serilog.Events;

// Enable Serilog self-log for debugging configuration issues
Serilog.Debugging.SelfLog.Enable(Console.Error);

// --- 1. BOOTSTRAP LOGGER (Serilog Two-Stage Initialization) ---
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting BillingFlow API Host...");

    var builder = WebApplication.CreateBuilder(args);

    // Replace built-in .NET Logger with Serilog via IServiceCollection
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // 2. --- CONFIGURE SERVICES (Dependency Injection) ---

    // Application Layer (MediatR, FluentValidation, Auth Policies)
    builder.Services.AddApplication();

    // Infrastructure Layer (EF Core, Identity, JWT Setup)
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    // API Specific Services (Controllers, Swagger, Exception Handler, Rate Limiting, OpenTelemetry)
    builder.Services.AddPresentation(builder.Configuration);

    // Custom Web Authorization (from AuthorizationExtensions.cs)
    builder.Services.AddWebAuthorization(builder.Configuration);

    var app = builder.Build();

    // --- 3. CONFIGURE HTTP REQUEST PIPELINE ---

    // 1. Resolve true client IP and Protocol from the NGINX Edge Gateway.
    app.UseForwardedHeaders();

    // 2. Catch and format all unhandled exceptions gracefully.
    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // app.UseHttpsRedirection() has been intentionally removed.
    // In this architecture, TLS/SSL termination is handled exclusively by the Edge Gateway (NGINX).
    // The internal .NET Kestrel server operates securely via plain HTTP within the isolated Docker bridge network.

    // 3. Endpoint Resolution (Identify which controller/action is being targeted)
    app.UseRouting();

    // 4. Identity Resolution (Parse JWT and populate context.User)
    app.UseAuthentication();

    // 5. Observability Context (Extract User and W3C TraceId into ambient LogContext & DiagnosticContext)
    // Placed after authentication so context.User is available
    app.UseMiddleware<RequestContextLoggingMiddleware>();

    // 6. Central Request Logging (Condenses HTTP pipeline events into a single enriched log entry)
    app.UseSerilogRequestLogging(options =>
    {
        // Custom format to be more readable
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

        // Smart leveling: 5xx is Error, 4xx is Warning, others Information
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null || httpContext.Response.StatusCode > 499)
                return LogEventLevel.Error;

            if (httpContext.Response.StatusCode > 399)
                return LogEventLevel.Warning;

            return LogEventLevel.Information;
        };

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    });

    // 7. Traffic Control (Evaluate User/IP limits BEFORE executing complex authorization policies)
    app.UseRateLimiter();

    // 8. Access Control (Verify if the identified, un-throttled user has the required permissions)
    app.UseAuthorization();

    // 9. Map Infrastructure & Application Endpoints
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = app.Environment.IsDevelopment()
            ? []
            : [new HangfireAuthorizationFilter()]
    });

    app.InitializeHangfireJobs();
    app.MapControllers();
    app.MapHub<ClientBalanceHub>("/hubs/balance");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BillingFlow API Host terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down BillingFlow API Host...");
    Log.CloseAndFlush();
}

public partial class Program { }
