// File: src/BillingFlow.Infrastructure/Database/Configurations/UserTokenConfiguration.cs
using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.HasKey(t => t.Id);

        // 1. UNIQUE Index on TokenHash: Guarantees one token -> one record and provides O(1) lookup
        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        // 2. Index on SessionId: Crucial for Logout and Aggressive Revocation queries
        builder.HasIndex(t => t.SessionId);

        // 3. Composite Index on Type + Expiry: Highly optimizes the background cleanup job
        builder.HasIndex(t => new { t.Type, t.Expiry });

        // 4. Optimistic Concurrency Token
        builder.Property(t => t.RowVersion)
            .IsRowVersion();
    }
}
