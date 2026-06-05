using BillingFlow.Infrastructure.Auditing;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.EntityId).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(20);

        builder.Property(a => a.UserId).IsRequired(false);
        builder.Property(a => a.TraceId).HasMaxLength(100).IsRequired(false); // e.g. W3C traceparent
        builder.Property(a => a.IpAddress).HasMaxLength(45).IsRequired(false);
        builder.Property(a => a.UserAgent).HasMaxLength(500).IsRequired(false);
        builder.Property(a => a.HttpMethod).HasMaxLength(10).IsRequired(false);
        builder.Property(a => a.RequestPath).HasMaxLength(500).IsRequired(false);

        builder.Property(a => a.OldValues).HasColumnType("nvarchar(max)").IsRequired(false);
        builder.Property(a => a.NewValues).HasColumnType("nvarchar(max)").IsRequired(false);

        builder.Property(a => a.Timestamp).IsRequired();

        // Index for tracing a specific entity's lifecycle
        builder.HasIndex(a => new { a.EntityName, a.EntityId });

        // Index for correlating errors/requests across systems
        builder.HasIndex(a => a.TraceId);
    }
}
