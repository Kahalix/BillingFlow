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

        builder.Property(ps => ps.PerformedAt)
            .IsRequired();

        builder.Property(ps => ps.BilledAt)
            .IsRequired(false);

        builder.Property(ps => ps.Status)
            .IsRequired();

        builder.HasIndex(ps => ps.ClientId);
        builder.HasIndex(ps => ps.InvoiceId);
        builder.HasIndex(ps => ps.Status);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(ps => ps.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(ps => ps.InvoiceId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
