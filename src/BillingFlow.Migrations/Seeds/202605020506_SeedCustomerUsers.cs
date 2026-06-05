using System;

using BillingFlow.Domain.Enums;

using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Seeds;

[Migration(202605020506)]
public class SeedCustomerUsers : Migration
{
    // Deterministic GUIDs for reliable relationship mapping in subsequent migrations
    private static readonly Guid Customer1Id = Guid.Parse("00000000-0000-0000-0000-000000000101");
    private static readonly Guid Customer2Id = Guid.Parse("00000000-0000-0000-0000-000000000102");

    public override void Up()
    {
        var createdAt = new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero);

        // Using the secure BCrypt hash representing "Admin@1234"
        const string defaultPasswordHash = "$2a$12$UQ6DLTv9nYIK3Igqck27duSF.gdDcWmRnfcJkAEIoZ2GEhQGViG2i";

        Execute.Sql($@"
            -- Seed Customer 1 (Active)
            IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = '{Customer1Id}')
            BEGIN
                INSERT INTO Users (Id, Email, PasswordHash, Role, Status, CreatedAt)
                VALUES (
                    '{Customer1Id}', 
                    'ceo@acmecorp.com', 
                    '{defaultPasswordHash}', 
                    {(int)Role.Customer}, 
                    {(int)UserStatus.Active}, 
                    '{createdAt:O}'
                );
            END

            -- Seed Customer 2 (Suspended)
            IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = '{Customer2Id}')
            BEGIN
                INSERT INTO Users (Id, Email, PasswordHash, Role, Status, CreatedAt)
                VALUES (
                    '{Customer2Id}', 
                    'finance@globex.com', 
                    '{defaultPasswordHash}', 
                    {(int)Role.Customer}, 
                    {(int)UserStatus.Suspended}, 
                    '{createdAt:O}'
                );
            END
        ");
    }

    public override void Down()
    {
        Delete.FromTable("Users").Row(new { Id = Customer1Id });
        Delete.FromTable("Users").Row(new { Id = Customer2Id });
    }
}
