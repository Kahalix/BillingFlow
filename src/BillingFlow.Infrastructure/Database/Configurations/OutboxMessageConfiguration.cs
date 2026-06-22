using BillingFlow.Infrastructure.Outbox.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

/// <summary>
/// EF Core mapping configuration for the infrastructural Outbox table.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Payload).IsRequired(); // Serialized JSON payload

        // Performance optimization: Composite index explicitly tailored for the background worker's polling query.
        // It covers Status, LockedUntil, and NextAttemptAt to ensure ultra-fast index seeks during batch claiming.
        builder.HasIndex(x => new { x.Status, x.LockedUntil, x.NextAttemptAt })
               .HasDatabaseName("IX_OutboxMessages_Worker_Polling");
    }
}
