// File: src/BillingFlow.Infrastructure/Database/BillingDbContext.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;

using Microsoft.Data.SqlClient;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Infrastructure.Database;

public class BillingDbContext(DbContextOptions<BillingDbContext> options)
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This will automatically apply all configurations (like UserTokenConfiguration)
        // that implement IEntityTypeConfiguration<T> in this assembly.
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
            // Extract the failing entity type directly from EF Core tracking.
            // This completely avoids parsing localized SQL Server error strings.
            var entityName = ex.Entries.FirstOrDefault()?.Metadata.ClrType.Name;

            throw new UniqueConstraintException(
                $"A database unique constraint was violated on entity: {entityName ?? "Unknown"}",
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
