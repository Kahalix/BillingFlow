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
        // Safe, highly predictable UPSERT pattern.
        // In extreme race conditions, an insert collision might throw an exception,
        // which would be caught and safely retried by Entity Framework's Execution Strategy.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ClientBalances 
            SET CurrentDebt = CurrentDebt + {deltaAmount}, UpdatedAt = {updatedAt} 
            WHERE ClientId = {clientId};

            IF @@ROWCOUNT = 0
            BEGIN
                -- Using TRY/CATCH at the SQL level to silently handle parallel insert collisions
                BEGIN TRY
                    INSERT INTO ClientBalances (ClientId, CurrentDebt, UpdatedAt) 
                    VALUES ({clientId}, {deltaAmount}, {updatedAt});
                END TRY
                BEGIN CATCH
                    IF ERROR_NUMBER() = 2627 -- Primary Key violation
                    BEGIN
                        -- Another thread inserted the row. Fallback to Update.
                        UPDATE ClientBalances 
                        SET CurrentDebt = CurrentDebt + {deltaAmount}, UpdatedAt = {updatedAt} 
                        WHERE ClientId = {clientId};
                    END
                    ELSE THROW;
                END CATCH
            END
            """, cancellationToken);
    }
}
