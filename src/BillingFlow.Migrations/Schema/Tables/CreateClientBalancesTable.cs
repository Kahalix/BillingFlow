// File: src/BillingFlow.Migrations/Schema/Tables/202605020007_CreateClientBalancesTable.cs
using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020007)]
public class CreateClientBalancesTable : Migration
{
    public override void Up()
    {
        Create.Table("ClientBalances")
            .WithColumn("ClientId").AsGuid().PrimaryKey() // PK is also the FK to Clients conceptually
            .WithColumn("CurrentDebt").AsDecimal(18, 2).NotNullable().WithDefaultValue(0m)
            .WithColumn("UpdatedAt").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("ClientBalances");
    }
}
