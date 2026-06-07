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
/// A critical scheduled job that scans the system for invoices that have missed their payment deadline.
/// Mutates the state through the Aggregate Root to ensure Domain Events and Audit Logs are properly generated.
/// </summary>
public class CheckOverdueInvoicesJob(
    IApplicationDbContext context,
    TimeProvider timeProvider,
    ILogger<CheckOverdueInvoicesJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // 1. O(1) Index-supported Query
        // Query for open invoices that have passed their due date.
        var targetInvoices = await context.Invoices
            .Where(i => (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid)
                     && i.DueDate < now)
            .ToListAsync(cancellationToken);

        if (!targetInvoices.Any())
        {
            return; // No actionable invoices found for this cycle.
        }

        // 2. Materialized State Mutation
        foreach (var invoice in targetInvoices)
        {
            try
            {
                // Mutating the aggregate root triggers state changes tracked by the EF Core ChangeTracker.
                invoice.MarkAsOverdue(now);
                logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) flagged as Overdue.", invoice.Id, invoice.InvoiceNumber);
            }
            catch (Exception ex)
            {
                // Fault isolation: if a single aggregate throws an exception, the remaining batch continues processing.
                logger.LogError(ex, "Failed to mark invoice {InvoiceId} as Overdue.", invoice.Id);
            }
        }

        // 3. Atomic Batch Transaction Commit
        // At this point, the AuditInterceptor captures the OldValues and NewValues as JSON to persist the historical audit trail.
        var savedCount = await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Completed overdue scan. Successfully transitioned {Count} invoices to Overdue status.", savedCount);
    }
}
