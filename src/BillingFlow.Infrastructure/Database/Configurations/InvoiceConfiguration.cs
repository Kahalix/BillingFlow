// File: src/BillingFlow.Infrastructure/Database/Configurations/InvoiceConfiguration.cs
using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        // 1. Core String Fields & Indexes
        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);

        builder.Property(i => i.OwnerUserId).IsRequired(false);

        builder.HasIndex(i => i.OwnerUserId);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique(); // SSOT invariant
        builder.HasIndex(i => i.ClientId);
        builder.HasIndex(i => i.Status);

        // 2. Financial Precision Constraints
        builder.Property(i => i.TotalAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.PaidAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // 3. Child Collection Mapping
        // By configuring this, EF Core knows to save/delete InvoiceItems alongside the Invoice
        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(i => i.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // 4. Concurrency Token
        builder.Property(i => i.RowVersion)
            .IsRowVersion();
    }
}
