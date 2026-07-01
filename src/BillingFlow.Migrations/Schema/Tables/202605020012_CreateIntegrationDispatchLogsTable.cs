using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020012)]
public class CreateIntegrationDispatchLogsTable : Migration
{
    public override void Up()
    {
        // 1. Table Schema Layout Specification
        Create.Table("IntegrationDispatchLogs")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("OutboxMessageId").AsGuid().NotNullable()
            .WithColumn("HandlerName").AsString(200).NotNullable()
            .WithColumn("Status").AsInt32().NotNullable() // Maps to DispatchStatus enum
            .WithColumn("UpdatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("LeaseToken").AsGuid().NotNullable()
            .WithColumn("LeaseExpiresAt").AsDateTimeOffset().NotNullable();

        // 2. Composite Unique Index Enforcement for Fan-Out Support
        // Guarantees one dispatch state machine per handler for a given Outbox message.
        // Prevents concurrent duplicate execution while allowing fan-out.
        Create.Index("IX_IntegrationDispatchLogs_OutboxMsgId_HandlerName")
            .OnTable("IntegrationDispatchLogs")
            .OnColumn("OutboxMessageId").Ascending()
            .OnColumn("HandlerName").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Table("IntegrationDispatchLogs");
    }
}
