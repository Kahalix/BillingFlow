using System.Net;

using BillingFlow.Api.Extensions;
using BillingFlow.Api.Hubs;
using BillingFlow.Api.Infrastructure;
using BillingFlow.Infrastructure.BackgroundJobs;

using Hangfire;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURE SERVICES (Dependency Injection) ---

// Application Layer (MediatR, FluentValidation, Auth Policies)
builder.Services.AddApplication();

// Infrastructure Layer (EF Core, Identity, JWT Setup)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// API Specific Services (Controllers, Swagger, Exception Handler, Rate Limiting)
builder.Services.AddPresentation(builder.Configuration);

// Add our custom Web Authorization (from AuthorizationExtensions.cs)
builder.Services.AddWebAuthorization(builder.Configuration);

var app = builder.Build();

// --- 2. CONFIGURE HTTP REQUEST PIPELINE ---

// PIPELINE ORDERING:
// 1. Resolve true client IP and Protocol from the NGINX Edge Gateway.
app.UseForwardedHeaders();

// 2. Catch and format all unhandled exceptions gracefully.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// PRODUCTION SECURITY REFACTOR:
// app.UseHttpsRedirection() has been intentionally removed.
// In this architecture, TLS/SSL termination is handled exclusively by the Edge Gateway (NGINX).
// The internal .NET Kestrel server operates securely via plain HTTP within the isolated Docker bridge network.

// 3. Endpoint Resolution (Identify which controller/action is being targeted)
app.UseRouting();

// 4. Identity Resolution (Parse JWT and populate context.User)
app.UseAuthentication();

// 5. Traffic Control (Evaluate User/IP limits BEFORE executing complex authorization policies)
app.UseRateLimiter();

// 6. Access Control (Verify if the identified, un-throttled user has the required permissions)
app.UseAuthorization();

// 7. Map Infrastructure & Application Endpoints
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

public partial class Program { }
