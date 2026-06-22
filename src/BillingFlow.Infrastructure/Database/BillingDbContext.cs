using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Infrastructure.Outbox.Models;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using AuditLog = BillingFlow.Infrastructure.Auditing.AuditLog;

namespace BillingFlow.Infrastructure.Database;

public class BillingDbContext(
    DbContextOptions<BillingDbContext> options,
    ILogger<BillingDbContext> logger)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<ProvidedService> ProvidedServices => Set<ProvidedService>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<StripeEventLog> StripeEventLogs => Set<StripeEventLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically apply all configuration mappings defined within this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Extract unique entities involved in the transaction, ignoring infrastructure audit records
            var entityNames = ex.Entries
                .Select(e => e.Metadata.ClrType.Name)
                .Where(name => name != nameof(AuditLog))
                .Distinct()
                .ToList();

            // Default to a neutral fallback if multi-entity conflicts occur to prevent structural information leakage
            var entityName = entityNames.Count == 1 ? entityNames[0] : "Unknown";

            // Extract native relational database error details for internal log context
            var sqlErrorMessage = ex.InnerException is SqlException sqlEx ? sqlEx.Message : "Unknown SQL details.";

            // Log full technical details securely on the server-side for troubleshooting
            logger.LogWarning(ex,
                "A database unique constraint violation occurred during SaveChangesAsync. Involved entities: {EntityNames}. SQL Details: {SqlErrorMessage}",
                string.Join(", ", entityNames),
                sqlErrorMessage);

            // Throw a sanitized application exception clear of low-level database infrastructure context
            throw new UniqueConstraintException(
                "The resource you are trying to create or update already exists or violates a uniqueness constraint.",
                entityName,
                ex);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            return sqlEx.Number is 2601 or 2627;
        }
        return false;
    }
}
