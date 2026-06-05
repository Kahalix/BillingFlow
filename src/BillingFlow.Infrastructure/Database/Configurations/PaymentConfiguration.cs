using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.InvoiceId);

        // CRITICAL: Filtered Unique Index to allow multiple manual payments (NULLs) 
        // while preventing double-processing of the same PaymentAttempt.
        builder.Property(p => p.PaymentAttemptId).IsRequired(false);
        builder.HasIndex(p => p.PaymentAttemptId)
            .IsUnique()
            .HasFilter("[PaymentAttemptId] IS NOT NULL");

        // Webhook Idempotency
        builder.Property(p => p.ExternalTransactionId).HasMaxLength(255);
        builder.HasIndex(p => p.ExternalTransactionId)
            .IsUnique()
            .HasFilter("[ExternalTransactionId] IS NOT NULL");

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
