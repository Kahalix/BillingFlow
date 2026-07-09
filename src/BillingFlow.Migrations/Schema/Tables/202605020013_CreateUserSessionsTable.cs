using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020013)]
public class CreateUserSessionsTable : Migration
{
    public override void Up()
    {
        Create.Table("UserSessions")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("ExpiresAt").AsDateTimeOffset().NotNullable()
            .WithColumn("AbsoluteExpiresAt").AsDateTimeOffset().NotNullable()
            .WithColumn("RevokedAt").AsDateTimeOffset().Nullable()
            .WithColumn("LastSeenAt").AsDateTimeOffset().Nullable()
            .WithColumn("IpAddress").AsString(45).Nullable()
            .WithColumn("UserAgent").AsString(512).Nullable()
            .WithColumn("RowVersion").AsCustom("rowversion").NotNullable();

        // Composite index optimized for session validation and LogoutAll queries
        Create.Index("IX_UserSessions_UserId_RevokedAt_ExpiresAt")
            .OnTable("UserSessions")
            .OnColumn("UserId").Ascending()
            .OnColumn("RevokedAt").Ascending()
            .OnColumn("ExpiresAt").Ascending();
    }

    public override void Down()
    {
        Delete.Table("UserSessions");
    }
}
