using BillingFlow.Infrastructure.Outbox.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

/// <summary>
/// EF Core mapping configuration for the infrastructural Outbox table.
/// Ensures reliable transactional outbox pattern execution.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(255).IsRequired();

        // Using nvarchar(max) for dynamic JSON payloads
        builder.Property(x => x.Payload).HasColumnType("nvarchar(max)").IsRequired();

        builder.Property(x => x.OccurredOn).IsRequired();
        builder.Property(x => x.ProcessedOn).IsRequired(false);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.LockedUntil).IsRequired(false);
        builder.Property(x => x.NextAttemptAt).IsRequired(false);
        builder.Property(x => x.AttemptCount).IsRequired();

        builder.Property(x => x.LastError).HasColumnType("nvarchar(max)").IsRequired(false);

        // Performance optimization: Composite index explicitly tailored for the background worker's polling query.
        // Covers Status, LockedUntil, and NextAttemptAt to ensure ultra-fast index seeks during batch claiming.
        builder.HasIndex(x => new { x.Status, x.LockedUntil, x.NextAttemptAt })
               .HasDatabaseName("IX_OutboxMessages_Worker_Polling");
    }
}
