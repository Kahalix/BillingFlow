using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BillingFlow.Infrastructure.Database.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId).IsRequired();
        builder.Property(s => s.CreatedAt).IsRequired();

        // Sliding and absolute bounds
        builder.Property(s => s.ExpiresAt).IsRequired();
        builder.Property(s => s.AbsoluteExpiresAt).IsRequired();

        builder.Property(s => s.RevokedAt).IsRequired(false);
        builder.Property(s => s.LastSeenAt).IsRequired(false);

        builder.Property(s => s.IpAddress).HasMaxLength(45).IsRequired(false);
        builder.Property(s => s.UserAgent).HasMaxLength(512).IsRequired(false);

        builder.Property(s => s.RowVersion).IsRowVersion();

        // Optimized composite index for high-performance session validation and LogoutAll queries
        builder.HasIndex(s => new { s.UserId, s.RevokedAt, s.ExpiresAt });

        // DDD-compliant foreign key configuration without exposing navigation collections
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
