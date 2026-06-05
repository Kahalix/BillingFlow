using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");

        builder.HasKey(c => c.Id);

        // 1. One User can only have ONE Client profile
        builder.Property(c => c.UserId)
            .IsRequired(false);

        builder.HasIndex(c => c.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        // Enforce the Enterprise Soft-Delete global query filter
        builder.HasQueryFilter(c => c.Status != ClientStatus.Archived);

        // 2. Company Details & Unique Tax Index mapping
        builder.Property(c => c.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.TaxId)
            .IsRequired()
            .HasMaxLength(50);

        // Enforce system-wide uniqueness on Tax ID to guard against double billing data configurations
        builder.HasIndex(c => c.TaxId)
            .IsUnique();

        // 3. Encapsulated Value Object Configuration (Owned Type)
        builder.OwnsOne(c => c.Address, a =>
        {
            a.Property(p => p.Street).HasColumnName("Street").HasMaxLength(150).IsRequired();
            a.Property(p => p.City).HasColumnName("City").HasMaxLength(100).IsRequired();
            a.Property(p => p.PostalCode).HasColumnName("PostalCode").HasMaxLength(20).IsRequired();
            a.Property(p => p.Country).HasColumnName("Country").HasMaxLength(100).IsRequired();
        });

        // 4. Status indexing for back-office querying optimization
        builder.HasIndex(c => c.Status);

        // 5. Inherited Concurrency Token configuration from Entity base class
        builder.Property(c => c.RowVersion)
            .IsRowVersion();
    }
}
