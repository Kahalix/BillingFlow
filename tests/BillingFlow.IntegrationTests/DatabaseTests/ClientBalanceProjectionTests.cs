using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Infrastructure.Database;
using BillingFlow.IntegrationTests.Base;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace BillingFlow.IntegrationTests.DatabaseTests;

public class ClientBalanceProjectionTests : BaseIntegrationTest
{
    public ClientBalanceProjectionTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ApplyDebtDeltaAsync_ConcurrentWritesOnExistingRecord_ShouldSumDeltasSafely()
    {
        // 1. Arrange Data
        var (_, client) = await DataFactory.CreateUserWithClientAsync();

        using (var setupScope = Factory.Services.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ClientBalances WHERE ClientId = {client.Id}");

            // BASELINE: The record exists before the race condition starts
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ClientBalances (ClientId, CurrentDebt, UpdatedAt) VALUES ({client.Id}, 0.00, {DateTimeOffset.UtcNow})");
        }

        using var scope1 = Factory.Services.CreateScope();
        var writer1 = scope1.ServiceProvider.GetRequiredService<IClientBalanceProjectionWriter>();

        using var scope2 = Factory.Services.CreateScope();
        var writer2 = scope2.ServiceProvider.GetRequiredService<IClientBalanceProjectionWriter>();

        // 2. Act: Two DbContexts racing to UPDATE the same row
        var task1 = writer1.ApplyDebtDeltaAsync(client.Id, -500m, DateTimeOffset.UtcNow, CancellationToken.None);
        var task2 = writer2.ApplyDebtDeltaAsync(client.Id, -500m, DateTimeOffset.UtcNow, CancellationToken.None);

        await Task.WhenAll(task1, task2);

        // 3. Assert
        using var finalScope = Factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var finalDebt = await finalDb.Database
            .SqlQuery<decimal?>($"SELECT CurrentDebt AS Value FROM ClientBalances WHERE ClientId = {client.Id}")
            .SingleOrDefaultAsync();

        finalDebt.Should().NotBeNull();
        finalDebt.Should().Be(-1000m);
    }

    [Fact]
    public async Task ApplyDebtDeltaAsync_ConcurrentWritesOnEmptyTable_ShouldCatchPrimaryKeyViolationAndFallbackToUpdate()
    {
        // 1. Arrange Data
        var (_, client) = await DataFactory.CreateUserWithClientAsync();

        using (var setupScope = Factory.Services.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<BillingDbContext>();
            // BASELINE: Strict empty state. The record DOES NOT exist.
            // This forces the SQL script into the TRY/CATCH (Insert) branch.
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ClientBalances WHERE ClientId = {client.Id}");
        }

        using var scope1 = Factory.Services.CreateScope();
        var writer1 = scope1.ServiceProvider.GetRequiredService<IClientBalanceProjectionWriter>();

        using var scope2 = Factory.Services.CreateScope();
        var writer2 = scope2.ServiceProvider.GetRequiredService<IClientBalanceProjectionWriter>();

        // 2. Act: Two DbContexts racing to INSERT the exact same Primary Key
        var task1 = writer1.ApplyDebtDeltaAsync(client.Id, 1000m, DateTimeOffset.UtcNow, CancellationToken.None);
        var task2 = writer2.ApplyDebtDeltaAsync(client.Id, 500m, DateTimeOffset.UtcNow, CancellationToken.None);

        await Task.WhenAll(task1, task2);

        // 3. Assert
        using var finalScope = Factory.Services.CreateScope();
        var finalDb = finalScope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var finalDebt = await finalDb.Database
            .SqlQuery<decimal?>($"SELECT CurrentDebt AS Value FROM ClientBalances WHERE ClientId = {client.Id}")
            .SingleOrDefaultAsync();

        // If the TRY/CATCH 2627 failed, this would either throw an exception or overwrite data (yielding 500 or 1000).
        // Since it works, the collision resolves to an atomic sum of 1500.
        finalDebt.Should().NotBeNull();
        finalDebt.Should().Be(1500m);
    }
}
