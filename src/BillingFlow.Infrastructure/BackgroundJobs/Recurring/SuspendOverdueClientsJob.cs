using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Infrastructure.BackgroundJobs.Recurring;

/// <summary>
/// A compliance job that enforces automated debt-collection policies.
/// Scans for active clients with overdue invoices and suspends their profiles.
/// </summary>
public class SuspendOverdueClientsJob(
    IApplicationDbContext context,
    ILogger<SuspendOverdueClientsJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        // 1. Single optimized query leveraging SQL EXISTS.
        var clientsToSuspend = await context.Clients
            .Where(c => c.Status == ClientStatus.Active &&
                        context.Invoices.Any(i => i.ClientId == c.Id && i.Status == InvoiceStatus.Overdue))
            .ToListAsync(cancellationToken);

        if (clientsToSuspend.Count == 0)
        {
            return;
        }

        int stagedCount = 0; // Tracks aggregates successfully mutated in memory

        // 2. Materialized State Mutation
        foreach (var client in clientsToSuspend)
        {
            try
            {
                client.Suspend();
                stagedCount++;

                logger.LogWarning(
                    "Client {ClientId} ({CompanyName}) staged for automated suspension.",
                    client.Id,
                    client.CompanyName);
            }
            catch (DomainException ex)
            {
                // Fault isolation prevents a single domain rule violation from failing the entire sweep,
                // while allowing critical system exceptions (e.g., NullReferenceException) to bubble up.
                logger.LogError(ex, "Domain validation failed. Could not stage suspension for client {ClientId}.", client.Id);
            }
        }

        if (stagedCount == 0)
        {
            return;
        }

        // 3. Atomic Batch Transaction Commit
        try
        {
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Automated debt collection sweep complete. Successfully committed suspension for {Count} clients out of {Total} fetched.",
                stagedCount,
                clientsToSuspend.Count);
        }
        catch (Exception ex)
        {
            // CRITICAL: Captures database-level failures.
            logger.LogError(ex, "CRITICAL: Database transaction failed during the automated debt collection sweep.");

            // Re-throw the exception so Hangfire can capture the failure and trigger an automatic retry.
            throw;
        }
    }
}
