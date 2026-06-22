using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020011)]
public class CreateOutboxMessagesTable : Migration
{
    public override void Up()
    {
        Create.Table("OutboxMessages")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Type").AsString(255).NotNullable()
            // Using int.MaxValue for NVARCHAR(MAX) to store dynamic JSON payloads of any size
            .WithColumn("Payload").AsString(int.MaxValue).NotNullable()
            .WithColumn("OccurredOn").AsDateTimeOffset().NotNullable()
            .WithColumn("ProcessedOn").AsDateTimeOffset().Nullable()
            .WithColumn("Status").AsInt32().NotNullable() // OutboxMessageStatus enum
            .WithColumn("LockedUntil").AsDateTimeOffset().Nullable()
            .WithColumn("NextAttemptAt").AsDateTimeOffset().Nullable()
            .WithColumn("AttemptCount").AsInt32().NotNullable()
            .WithColumn("LastError").AsString(int.MaxValue).Nullable();

        // Composite index explicitly tailored for the background worker's polling query.
        // It covers Status, LockedUntil, and NextAttemptAt to ensure ultra-fast index seeks 
        // during batch claiming (UPDLOCK, READPAST), preventing full table scans.
        Create.Index("IX_OutboxMessages_Worker_Polling")
            .OnTable("OutboxMessages")
            .OnColumn("Status").Ascending()
            .OnColumn("LockedUntil").Ascending()
            .OnColumn("NextAttemptAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("OutboxMessages");
    }
}
