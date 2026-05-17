// File: src/BillingFlow.Api/Program.cs
using BillingFlow.Api.Extensions;
using BillingFlow.Api.Infrastructure;
using Hangfire;
using BillingFlow.Infrastructure.BackgroundJobs;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURE SERVICES (Dependency Injection) ---

// Application Layer (MediatR, FluentValidation, Auth Policies)
builder.Services.AddApplication();

// Infrastructure Layer (EF Core, Identity, JWT Setup)
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

// Migrations Layer (FluentMigrator)
// builder.Services.AddBillingMigrations(builder.Configuration);

// API Specific Services (Controllers, Swagger, Exception Handler, Rate Limiting)
builder.Services.AddPresentation();

// Add our custom Web Authorization (from AuthorizationExtensions.cs)
builder.Services.AddWebAuthorization(builder.Configuration);

var app = builder.Build();

// --- 2. CONFIGURE HTTP REQUEST PIPELINE ---

// Global Exception Handler MUST be first in the pipeline
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Routing & Rate Limiting (MUST be in this order and before Auth)
app.UseRouting();
app.UseRateLimiter();

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// --- Hangfire Dashboard Mapping ---
// We map it globally, but apply different authorization rules based on the environment.
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = app.Environment.IsDevelopment()
        ? [] // Local dev: No auth required
        : [new HangfireAuthorizationFilter()] // Production: Only Admins
});

app.MapControllers();

app.Run();
