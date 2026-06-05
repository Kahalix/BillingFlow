using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;

using Dapper;

namespace BillingFlow.Infrastructure.Invoices;

public class SequenceInvoiceNumberGenerator(
    IDbConnectionFactory connectionFactory,
    TimeProvider timeProvider) : IInvoiceNumberGenerator
{
    public async Task<string> GenerateNextNumberAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // 1. Open a lightweight connection
        using var connection = connectionFactory.CreateConnection();

        // 2. Fetch the next value from the SQL Server sequence
        const string sql = "SELECT NEXT VALUE FOR InvoiceNumberSequence;";

        // Note: Dapper handles cancellation via CommandDefinition
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var nextSequenceValue = await connection.QuerySingleAsync<long>(command);

        // 3. Format the Invoice Number (e.g., "INV/2026/05/00001")
        // D5 means zero-padded to 5 digits
        return $"INV/{now:yyyy}/{now:MM:00}/{nextSequenceValue:D5}";
    }
}
