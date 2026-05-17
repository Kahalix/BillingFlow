// File: src/BillingFlow.Migrations/Program.cs
using System;

using BillingFlow.Migrations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("=======================================");
Console.WriteLine(" BillingFlow Database Migrator Started");
Console.WriteLine("=======================================");

// 1. Build configuration (Sets up DI, Console Logging, and Environment Variables automatically)
var builder = Host.CreateApplicationBuilder(args);

// 2. Setup Dependency Injection
// Add FluentMigrator (from DependencyInjection.cs)
builder.Services.AddBillingMigrations(builder.Configuration);

// Add our custom orchestration class
builder.Services.AddTransient<MigrationRunner>();

using var host = builder.Build();

// 3. Execute the Migrations
try
{
    using var scope = host.Services.CreateScope();
    
    var runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
    runner.Run();

    Console.WriteLine("=======================================");
    Console.WriteLine(" Migrations completed successfully!");
    Console.WriteLine("=======================================");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"FATAL MIGRATION ERROR: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Console.ResetColor();

    // CRITICAL: Exit with a non-zero code. 
    // This tells Docker Compose that the migrator FAILED, preventing the API from starting.
    Environment.Exit(1);
}
