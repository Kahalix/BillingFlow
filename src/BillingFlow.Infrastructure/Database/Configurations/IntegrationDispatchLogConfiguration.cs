using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Infrastructure.Auditing;
using BillingFlow.Infrastructure.Outbox.Models;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class IntegrationDispatchLogConfiguration : IEntityTypeConfiguration<IntegrationDispatchLog>
{
    public void Configure(EntityTypeBuilder<IntegrationDispatchLog> builder)
    {
        builder.ToTable("IntegrationDispatchLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.HandlerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.LeaseToken)
            .IsRequired();

        builder.Property(x => x.LeaseExpiresAt)
            .IsRequired();

        // UNIQUE CONSTRAINT - Composite key for Fan-Out support.
        // Allows the same Outbox message to trigger multiple independent handlers (e.g., Email AND SignalR)
        builder.HasIndex(x => new { x.OutboxMessageId, x.HandlerName })
            .IsUnique();

        builder.HasOne<OutboxMessage>()
            .WithMany()
            .HasForeignKey(x => x.OutboxMessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
