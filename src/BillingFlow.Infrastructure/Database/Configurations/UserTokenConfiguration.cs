using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");

        builder.HasKey(t => t.Id);

        // 1. UNIQUE Index on TokenHash: Guarantees one token -> one record and provides O(1) lookup
        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        // 2. Composite Index for Logout & Revocation checks
        // Covers queries like: t.SessionId == sessionId && t.Type == UserTokenType.RefreshToken && t.ConsumedAt == null
        builder.HasIndex(t => new { t.SessionId, t.Type, t.ConsumedAt });

        // 3. Composite Index for LogoutAllDevices & Account Suspensions
        // Covers queries like: t.UserId == userId && t.ConsumedAt == null
        builder.HasIndex(t => new { t.UserId, t.Type, t.ConsumedAt });

        // 4. Single Index for Background Cleanup Jobs
        // SQL Server left-prefix rule dictates that querying solely by 'Expiry' requires 'Expiry' to be the leading column.
        builder.HasIndex(t => t.Expiry);

        builder.Property(t => t.ConsumedAt)
            .IsRequired(false);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.Data)
            .HasMaxLength(255)
            .IsRequired(false);

        // Required to catch DbUpdateConcurrencyException during Refresh Token replay attempts
        builder.Property(t => t.RowVersion)
            .IsRowVersion();

        // DDD-compliant foreign keys (Restrict to preserve enterprise security audit trails)
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UserSession>()
            .WithMany()
            .HasForeignKey(t => t.SessionId)
            .IsRequired(false) // SessionId is nullable
            .OnDelete(DeleteBehavior.Restrict);
    }
}
