using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020006)]
public class CreateProvidedServicesTable : Migration
{
    public override void Up()
    {
        Create.Table("ProvidedServices")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("ClientId").AsGuid().NotNullable()
            .WithColumn("Description").AsString(500).NotNullable()
            .WithColumn("Amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("PerformedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("InvoiceId").AsGuid().Nullable()
            .WithColumn("BilledAt").AsDateTimeOffset().Nullable();

        Create.Index("IX_ProvidedServices_ClientId")
            .OnTable("ProvidedServices").OnColumn("ClientId").Ascending();

        Create.Index("IX_ProvidedServices_InvoiceId")
            .OnTable("ProvidedServices").OnColumn("InvoiceId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("ProvidedServices");
    }
}
