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

        // 4. Composite Index for Background Cleanup Jobs
        // Covers queries filtering by expired tokens of specific types
        builder.HasIndex(t => new { t.Type, t.Expiry });

        builder.Property(t => t.ConsumedAt)
            .IsRequired(false);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.Data)
            .HasMaxLength(255) 
            .IsRequired(false);

    }
}
