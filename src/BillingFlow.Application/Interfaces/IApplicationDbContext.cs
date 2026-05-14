// File: src/BillingFlow.Application/Interfaces/IApplicationDbContext.cs
using BillingFlow.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<UserToken> UserTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
