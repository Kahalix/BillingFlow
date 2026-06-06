using System;

using BillingFlow.Domain.Enums;

using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Seeds;

[Migration(202605020508)]
public class SeedProvidedServices : Migration
{
    private static readonly Guid Service1Id = Guid.Parse("00000000-0000-0000-0000-000000000301");
    private static readonly Guid Service2Id = Guid.Parse("00000000-0000-0000-0000-000000000302");

    // Reference to Client 1 (Acme Corporation)
    private static readonly Guid Client1Id = Guid.Parse("00000000-0000-0000-0000-000000000201");

    public override void Up()
    {
        var now = new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero);
        var lastWeek = now.AddDays(-7);

        Execute.Sql($@"
            -- Service 1: IT Consulting (Not billed yet, ready to be grouped into an invoice)
            IF NOT EXISTS (SELECT 1 FROM ProvidedServices WHERE Id = '{Service1Id}')
            BEGIN
                INSERT INTO ProvidedServices (Id, ClientId, InvoiceId, Description, Amount, PerformedAt, Status)
                VALUES (
                    '{Service1Id}', 
                    '{Client1Id}', 
                    NULL, 
                    'Enterprise Cloud Migration Strategy Consulting', 
                    1500.00, 
                    '{lastWeek:O}',
                    {(int)ProvidedServiceStatus.Unbilled}
                );
            END

            -- Service 2: Server Maintenance
            IF NOT EXISTS (SELECT 1 FROM ProvidedServices WHERE Id = '{Service2Id}')
            BEGIN
                INSERT INTO ProvidedServices (Id, ClientId, InvoiceId, Description, Amount, PerformedAt, Status)
                VALUES (
                    '{Service2Id}', 
                    '{Client1Id}', 
                    NULL, 
                    'Monthly AWS Server Maintenance and Security Patching', 
                    450.50, 
                    '{lastWeek.AddDays(1):O}',
                    {(int)ProvidedServiceStatus.Unbilled}
                );
            END
        ");
    }

    public override void Down()
    {
        Delete.FromTable("ProvidedServices").Row(new { Id = Service1Id });
        Delete.FromTable("ProvidedServices").Row(new { Id = Service2Id });
    }
}
