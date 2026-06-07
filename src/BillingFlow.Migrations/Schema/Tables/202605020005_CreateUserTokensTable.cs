using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020005)]
public class CreateUserTokensTable : Migration
{
    public override void Up()
    {
        Create.Table("UserTokens")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("UserId").AsGuid().NotNullable()
            .WithColumn("TokenHash").AsString(128).NotNullable()
            .WithColumn("SessionId").AsGuid().NotNullable()
            .WithColumn("Type").AsInt32().NotNullable()
            .WithColumn("Expiry").AsDateTimeOffset().NotNullable()
            .WithColumn("ConsumedAt").AsDateTimeOffset().Nullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("Data").AsString(255).Nullable();

        // 1. Unique Index on TokenHash
        Create.Index("IX_UserTokens_TokenHash")
            .OnTable("UserTokens")
            .OnColumn("TokenHash").Ascending()
            .WithOptions().Unique();

        // 2. Composite Index for Session Revocation
        Create.Index("IX_UserTokens_SessionId_Type_ConsumedAt")
            .OnTable("UserTokens")
            .OnColumn("SessionId").Ascending()
            .OnColumn("Type").Ascending()
            .OnColumn("ConsumedAt").Ascending();

        // 3. Composite Index for User-level Revocation
        Create.Index("IX_UserTokens_UserId_Type_ConsumedAt")
            .OnTable("UserTokens")
            .OnColumn("UserId").Ascending()
            .OnColumn("Type").Ascending()
            .OnColumn("ConsumedAt").Ascending();


        // 4. Single Index for Cleanup Jobs (Optimized for WHERE Expiry <= @now)
        Create.Index("IX_UserTokens_Expiry")
            .OnTable("UserTokens")
            .OnColumn("Expiry").Ascending();

        //// 4. Composite Index for Cleanup Jobs
        //Create.Index("IX_UserTokens_Type_Expiry")
        //    .OnTable("UserTokens")
        //    .OnColumn("Type").Ascending()
        //    .OnColumn("Expiry").Ascending();
    }

    public override void Down()
    {
        Delete.Table("UserTokens");
    }
}
