using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class ProvidedServiceConfiguration : IEntityTypeConfiguration<ProvidedService>
{
    public void Configure(EntityTypeBuilder<ProvidedService> builder)
    {
        builder.ToTable("ProvidedServices");
        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ps => ps.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasIndex(ps => ps.ClientId);

        // Critical for fast lookups of uninvoiced services
        builder.HasIndex(ps => ps.InvoiceId);
    }
}
