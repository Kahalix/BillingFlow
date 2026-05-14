// File: src/BillingFlow.Infrastructure/Database/Configurations/AppUserConfiguration.cs
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
        // 1. Primary Key
        builder.HasKey(u => u.Id);

        // 2. Email Configuration
        // MaxLength(255) aligns with the standard RFC 5321 length for email addresses.
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        // Unique index ensures absolute database-level protection against duplicate accounts.
        // Provides O(log n) lookup performance for the login process.
        builder.HasIndex(u => u.Email)
            .IsUnique();

        // 3. Password Hash Configuration
        // BCrypt hashes are consistently 60 characters long. 
        // 128 provides a safe buffer in case the hashing algorithm changes in the future (e.g., Argon2).
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(128);

        // 4. Query Optimization
        // Composite index to speed up back-office queries (like GetUsersQuery) filtering by Role and Status.
        builder.HasIndex(u => new { u.Role, u.Status });

        // 5. Optimistic Concurrency
        // Maps the RowVersion property (inherited from the Entity base class) to SQL Server's ROWVERSION.
        // Crucial for preventing lost updates when multiple admins edit the same user concurrently.
        builder.Property(u => u.RowVersion)
            .IsRowVersion();

        // 6. Relationships
        // Explicitly define the One-to-Many relationship with UserToken.
        // Cascade delete ensures that if an AppUser is hard-deleted, all their tokens are wiped out automatically.
        builder.HasMany<UserToken>()
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
