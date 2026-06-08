using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Infrastructure.BackgroundJobs.Recurring;

/// <summary>
/// A technical garbage collection job that routinely sweeps the database for expired security tokens.
/// Uses set-based bulk deletes (EF Core ExecuteDelete) for maximum performance and minimal memory footprint.
/// </summary>
public class CleanupExpiredTokensJob(
    IApplicationDbContext context,
    TimeProvider timeProvider,
    ILogger<CleanupExpiredTokensJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // 1. O(1) Index-supported Bulk Delete
        // CRITICAL SECURITY NOTE: We ONLY delete tokens that have surpassed their natural expiration date.
        // We strictly DO NOT aggressively delete unexpired tokens simply because they are marked as 'Consumed'.
        // Retaining consumed tokens until their natural Expiry is required to detect Refresh Token Reuse attacks.
        var deletedCount = await context.UserTokens
            .Where(t => t.Expiry <= now)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount > 0)
        {
            logger.LogInformation("Security maintenance sweep complete. Successfully purged {Count} expired tokens.", deletedCount);
        }
    }
}
