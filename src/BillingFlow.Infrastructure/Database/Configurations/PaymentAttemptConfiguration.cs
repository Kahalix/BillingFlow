using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("PaymentAttempts");
        builder.HasKey(pa => pa.Id);

        // CONCURRENCY LOCK (Idempotency against double-clicks)
        // SQL Server natively supports IN clauses in filtered indexes
        builder.HasIndex(pa => pa.InvoiceId)
            .IsUnique()
            .HasFilter($"[Status] IN ({(int)PaymentStatus.Initializing}, {(int)PaymentStatus.Started})");

        // 2. Provider specific unique constraints (Composite Index)
        // Ensures ProviderReference is unique PER PROVIDER (e.g., Stripe's Session ID won't clash with PayPal's Order ID)
        builder.Property(pa => pa.ProviderReference).IsRequired(false).HasMaxLength(255);

        builder.HasIndex(pa => new { pa.Provider, pa.ProviderReference })
            .IsUnique()
            .HasFilter("[ProviderReference] IS NOT NULL");

        builder.Property(pa => pa.CheckoutUrl).IsRequired(false).HasMaxLength(2000);

        builder.Property(pa => pa.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(pa => pa.ErrorMessage).HasMaxLength(1000);
    }
}
