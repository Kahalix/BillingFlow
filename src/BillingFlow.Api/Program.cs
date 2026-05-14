// File: src/BillingFlow.Api/Program.cs
using BillingFlow.Api.Extensions;
using BillingFlow.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURE SERVICES (Dependency Injection) ---

// Application Layer (MediatR, FluentValidation, Auth Policies)
builder.Services.AddApplication();

// Infrastructure Layer (EF Core, Identity, JWT Setup)
builder.Services.AddInfrastructure(builder.Configuration);

// Migrations Layer (FluentMigrator)
//builder.Services.AddBillingMigrations(builder.Configuration);

// API Specific Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Modern Exception Handling (.NET 8 standard)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
