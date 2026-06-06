using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020002)]
public class CreateClientsTable : Migration
{
    public override void Up()
    {
        Create.Table("Clients")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().Nullable()
            .WithColumn("CompanyName").AsString(200).NotNullable()
            .WithColumn("TaxId").AsString(50).NotNullable()
            .WithColumn("Street").AsString(150).NotNullable()
            .WithColumn("City").AsString(100).NotNullable()
            .WithColumn("PostalCode").AsString(20).NotNullable()
            .WithColumn("Country").AsString(100).NotNullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("RowVersion").AsCustom("rowversion").NotNullable();

        // Create a filtered unique index using raw SQL Server syntax.
        // This ensures UserId uniqueness only when it is NOT NULL, allowing multiple clients to exist without a user.
        Execute.Sql("CREATE UNIQUE NONCLUSTERED INDEX [IX_Clients_UserId] ON [dbo].[Clients] ([UserId] ASC) WHERE [UserId] IS NOT NULL;");

        // Enforce absolute database protection against duplicates using TaxId
        Create.Index("IX_Clients_TaxId")
            .OnTable("Clients")
            .OnColumn("TaxId").Ascending()
            .WithOptions().Unique();

        // Status queries lookup speed-up index configuration
        Create.Index("IX_Clients_Status")
            .OnTable("Clients")
            .OnColumn("Status").Ascending();
    }

    public override void Down()
    {
        Delete.Table("Clients");
    }
}
