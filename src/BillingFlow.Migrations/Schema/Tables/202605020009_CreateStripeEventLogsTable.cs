using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020009)]
public class CreateStripeEventLogsTable : Migration
{
    public override void Up()
    {
        Create.Table("StripeEventLogs")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("EventId").AsString(255).NotNullable()
            .WithColumn("ProcessedAt").AsDateTimeOffset().NotNullable();

        // UNIQUE INDEX: Hard database block against concurrent duplicate webhook waves
        Create.Index("IX_StripeEventLogs_EventId")
            .OnTable("StripeEventLogs")
            .OnColumn("EventId").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("StripeEventLogs");
    }
}
