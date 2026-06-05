// File: src/BillingFlow.Infrastructure/BackgroundJobs/HangfireExtensions.cs
using Hangfire;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using BillingFlow.Infrastructure.BackgroundJobs.Recurring;

namespace BillingFlow.Infrastructure.BackgroundJobs;

/// <summary>
/// Encapsulates the runtime initialization of Hangfire recurring jobs.
/// Keeps the Program.cs clean and delegates infrastructure knowledge to the Infrastructure layer.
/// </summary>
public static class HangfireExtensions
{
    public static WebApplication InitializeHangfireJobs(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        // Garbage Collection: Expires stale checkout sessions to free up database locks
        recurringJobManager.AddOrUpdate<CleanupExpiredPaymentAttemptsJob>(
            "cleanup-expired-payment-attempts",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/10 * * * *" // Run every 10 minutes
        );

        // Add future recurring jobs here...

        return app;
    }
}
