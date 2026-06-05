using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Infrastructure.BackgroundJobs.Recurring;

/// <summary>
/// A garbage collection job that routinely sweeps the database for expired payment reservations.
/// Uses set-based bulk updates (EF Core ExecuteUpdate) for maximum performance and minimal memory footprint.
/// </summary>
public class CleanupExpiredPaymentAttemptsJob(
    IApplicationDbContext context,
    TimeProvider timeProvider,
    ILogger<CleanupExpiredPaymentAttemptsJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // Performs a single, highly-optimized UPDATE query directly on the database.
        // Bypasses loading entities into memory, which is ideal for bulk garbage collection.
        var affected = await context.PaymentAttempts
            .Where(pa => (pa.Status == PaymentStatus.Initializing || pa.Status == PaymentStatus.Started)
                      && pa.ExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(pa => pa.Status, PaymentStatus.Expired)
                .SetProperty(pa => pa.CheckoutUrl, (string?)null)
                .SetProperty(pa => pa.ProviderReference, (string?)null),
            cancellationToken);

        if (affected > 0)
        {
            logger.LogInformation("Successfully cleaned up and expired {Count} stale payment attempts.", affected);
        }
    }
}
