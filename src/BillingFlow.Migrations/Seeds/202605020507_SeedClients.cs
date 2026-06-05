using System;

using BillingFlow.Domain.Enums;

using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Seeds;

[Migration(202605020507)]
public class SeedClients : Migration
{
    // Deterministic GUIDs for the Client aggregates
    private static readonly Guid Client1Id = Guid.Parse("00000000-0000-0000-0000-000000000201");
    private static readonly Guid Client2Id = Guid.Parse("00000000-0000-0000-0000-000000000202");
    private static readonly Guid Client3Id = Guid.Parse("00000000-0000-0000-0000-000000000203");

    // References to the Users created in the previous seed
    private static readonly Guid Customer1UserId = Guid.Parse("00000000-0000-0000-0000-000000000101");
    private static readonly Guid Customer2UserId = Guid.Parse("00000000-0000-0000-0000-000000000102");

    public override void Up()
    {
        Execute.Sql($@"
            -- Client 1: Active, linked to Customer 1
            IF NOT EXISTS (SELECT 1 FROM Clients WHERE Id = '{Client1Id}')
            BEGIN
                INSERT INTO Clients (Id, UserId, CompanyName, TaxId, Street, City, PostalCode, Country, Status)
                VALUES (
                    '{Client1Id}', 
                    '{Customer1UserId}', 
                    'Acme Corporation', 
                    'US123456789', 
                    '123 Innovation Drive', 
                    'San Francisco', 
                    '94105', 
                    'USA', 
                    {(int)ClientStatus.Active}
                );
            END

            -- Client 2: Suspended, linked to Customer 2
            IF NOT EXISTS (SELECT 1 FROM Clients WHERE Id = '{Client2Id}')
            BEGIN
                INSERT INTO Clients (Id, UserId, CompanyName, TaxId, Street, City, PostalCode, Country, Status)
                VALUES (
                    '{Client2Id}', 
                    '{Customer2UserId}', 
                    'Globex Inc.', 
                    'UK987654321', 
                    '45 Business Way', 
                    'London', 
                    'E1 6AN', 
                    'United Kingdom', 
                    {(int)ClientStatus.Suspended}
                );
            END

            -- Client 3: Archived, unlinked (UserId is NULL) - Ready to be tested with Restore/Link commands
            IF NOT EXISTS (SELECT 1 FROM Clients WHERE Id = '{Client3Id}')
            BEGIN
                INSERT INTO Clients (Id, UserId, CompanyName, TaxId, Street, City, PostalCode, Country, Status)
                VALUES (
                    '{Client3Id}', 
                    NULL, 
                    'Initech', 
                    'PL111222333', 
                    'ul. Programistow 1', 
                    'Warsaw', 
                    '00-001', 
                    'Poland', 
                    {(int)ClientStatus.Archived}
                );
            END
        ");
    }

    public override void Down()
    {
        Delete.FromTable("Clients").Row(new { Id = Client1Id });
        Delete.FromTable("Clients").Row(new { Id = Client2Id });
        Delete.FromTable("Clients").Row(new { Id = Client3Id });
    }
}
