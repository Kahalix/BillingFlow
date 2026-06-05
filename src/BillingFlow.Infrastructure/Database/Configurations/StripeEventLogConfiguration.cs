// File: src/BillingFlow.Infrastructure/Database/Configurations/StripeEventLogConfiguration.cs
using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

/// <summary>
/// Infrastructure mapping definition for the StripeEventLog database structure.
/// Explicitly declares indices to guarantee cross-network idempotent transactions.
/// </summary>
public class StripeEventLogConfiguration : IEntityTypeConfiguration<StripeEventLog>
{
    public void Configure(EntityTypeBuilder<StripeEventLog> builder)
    {
        // 1. Table Layout Specification
        builder.ToTable("StripeEventLogs");

        // 2. Primary Key Configuration
        builder.HasKey(x => x.Id);

        // 3. Properties and Boundaries Configuration
        builder.Property(x => x.EventId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.ProcessedAt)
            .IsRequired();

        // 4. Unique Index Enforcement
        // Prevents two concurrent asynchronous HTTP webhook threads from processing the same event.
        // If an identity clash happens, SQL Server terminates the operation and returns error 2601/2627.
        builder.HasIndex(x => x.EventId)
            .IsUnique();
    }
}
