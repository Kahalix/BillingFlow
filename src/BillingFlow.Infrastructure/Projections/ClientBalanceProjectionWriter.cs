// File: src/BillingFlow.Infrastructure/Projections/ClientBalanceProjectionWriter.cs
using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Infrastructure.Database;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Infrastructure.Projections;

public class ClientBalanceProjectionWriter(BillingDbContext context) : IClientBalanceProjectionWriter
{
    public async Task ApplyDebtDeltaAsync(Guid clientId, decimal deltaAmount, DateTimeOffset updatedAt, CancellationToken cancellationToken = default)
    {
        // High-performance UPSERT. 
        // If deltaAmount is negative (payment), it naturally subtracts from CurrentDebt.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ClientBalances 
            SET CurrentDebt = CurrentDebt + {deltaAmount}, UpdatedAt = {updatedAt} 
            WHERE ClientId = {clientId};

            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO ClientBalances (ClientId, CurrentDebt, UpdatedAt) 
                VALUES ({clientId}, {deltaAmount}, {updatedAt});
            END
            """, cancellationToken);
    }
}
