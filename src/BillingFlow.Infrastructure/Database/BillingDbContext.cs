// File: src/BillingFlow.Infrastructure/Database/BillingDbContext.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Infrastructure.Database;

public class BillingDbContext(DbContextOptions<BillingDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // This will automatically apply all configurations (like UserTokenConfiguration)
        // that implement IEntityTypeConfiguration<T> in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
