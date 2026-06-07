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
        var options = new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc };

        // 1. Technical Garbage Collection
        // Expires stale checkout sessions to free up database locks
        recurringJobManager.AddOrUpdate<CleanupExpiredPaymentAttemptsJob>(
            "cleanup-expired-payment-attempts",
            job => job.ExecuteAsync(CancellationToken.None),
            "*/10 * * * *" // Run every 10 minutes
        );

        // 2. Financial Operations
        recurringJobManager.AddOrUpdate<CheckOverdueInvoicesJob>(
            "check-overdue-invoices",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 0 * * *", // Run daily at midnight (UTC)
            options // Explicit UTC enforcing
        );

        // 3. Compliance & Enforcement
        recurringJobManager.AddOrUpdate<SuspendOverdueClientsJob>(
            "suspend-overdue-clients",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 1 * * *", // Run daily at 01:00 AM (UTC)
            options // Explicit UTC enforcing
        );

        // Add future recurring jobs here...

        return app;
    }
}
