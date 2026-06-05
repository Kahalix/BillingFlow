// File: src/BillingFlow.Migrations/Schema/Seeds/202605020510_SeedPaymentsAndBalances.cs
using System;

using BillingFlow.Domain.Enums;

using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Seeds;

[Migration(202605020510)]
public class SeedPaymentsAndBalances : Migration
{
    private static readonly Guid Payment1Id = Guid.Parse("00000000-0000-0000-0000-000000000601");
    private static readonly Guid Invoice2Id = Guid.Parse("00000000-0000-0000-0000-000000000402");

    private static readonly Guid Client1Id = Guid.Parse("00000000-0000-0000-0000-000000000201");

    // We reference the seeded Admin User ID to maintain audit integrity
    // for manual back-office payments, mirroring our Domain constraints.
    private static readonly Guid AdminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public override void Up()
    {
        var now = new DateTimeOffset(2026, 5, 2, 10, 0, 0, TimeSpan.Zero);

        Execute.Sql($@"
            -- Create a manual payment corresponding to the 500.00 'PaidAmount' of Invoice 2
            IF NOT EXISTS (SELECT 1 FROM Payments WHERE Id = '{Payment1Id}')
            BEGIN
                INSERT INTO Payments (Id, InvoiceId, PaymentAttemptId, Amount, PaymentDate, Provider, Method, ExternalTransactionId, ReceivedByUserId, Notes, CreatedAt)
                VALUES (
                    '{Payment1Id}', 
                    '{Invoice2Id}', 
                    NULL, 
                    500.00, 
                    '{now:O}', 
                    {(int)PaymentProvider.BackOffice}, 
                    {(int)PaymentMethod.BankTransfer}, 
                    NULL, 
                    '{AdminUserId}', -- Set to Admin instead of NULL
                    'Initial down-payment via manual bank wire', 
                    '{now:O}'
                );
            END

            -- Materialize the ClientBalances Read Model
            -- Client 1 has Invoice 1 (2000 debt) and Invoice 2 (1500 debt - 500 paid = 1000 debt). Total Debt: 3000.
            IF NOT EXISTS (SELECT 1 FROM ClientBalances WHERE ClientId = '{Client1Id}')
            BEGIN
                INSERT INTO ClientBalances (ClientId, CurrentDebt, UpdatedAt)
                VALUES ('{Client1Id}', 3000.00, '{now:O}');
            END
        ");
    }

    public override void Down()
    {
        Delete.FromTable("ClientBalances").Row(new { ClientId = Client1Id });
        Delete.FromTable("Payments").Row(new { Id = Payment1Id });
    }
}
