using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

/// <summary>
/// Entity Framework Core configuration for the AppUser aggregate root.
/// Enforces database-level constraints, indexes, and relationships.
/// </summary>
public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");

        // 1. Primary Key
        builder.HasKey(u => u.Id);

        // 2. Email Configuration
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        // 3. Password Hash Configuration
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(128);

        // 4. Query Optimization
        builder.HasIndex(u => new { u.Role, u.Status });

        // 5. Optimistic Concurrency
        builder.Property(u => u.RowVersion)
            .IsRowVersion();

        // 6. Relationships
        builder.HasMany<UserToken>()
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
