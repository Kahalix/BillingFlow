using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020010)]
public class CreateAuditLogsTable : Migration
{
    public override void Up()
    {
        Create.Table("AuditLogs")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("EntityName").AsString(100).NotNullable()
            .WithColumn("EntityId").AsString(50).NotNullable()
            .WithColumn("Action").AsString(20).NotNullable()
            .WithColumn("UserId").AsGuid().Nullable()
            .WithColumn("TraceId").AsString(100).Nullable()
            .WithColumn("IpAddress").AsString(45).Nullable()
            .WithColumn("UserAgent").AsString(500).Nullable()
            .WithColumn("HttpMethod").AsString(10).Nullable()
            .WithColumn("RequestPath").AsString(500).Nullable()
            .WithColumn("OldValues").AsString(int.MaxValue).Nullable()
            .WithColumn("NewValues").AsString(int.MaxValue).Nullable()
            .WithColumn("Timestamp").AsDateTimeOffset().NotNullable();

        Create.Index("IX_AuditLogs_EntityName_EntityId")
            .OnTable("AuditLogs")
            .OnColumn("EntityName").Ascending()
            .OnColumn("EntityId").Ascending();

        Create.Index("IX_AuditLogs_TraceId")
            .OnTable("AuditLogs")
            .OnColumn("TraceId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("AuditLogs");
    }
}
