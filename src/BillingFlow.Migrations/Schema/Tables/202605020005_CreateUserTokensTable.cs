// File: src/BillingFlow.Migrations/Schema/Tables/202605020005_CreateUserTokensTable.cs
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
            // Reduced to 128 characters (sufficient for SHA-256 hex string which is 64 chars)
            .WithColumn("TokenHash").AsString(128).NotNullable()
            .WithColumn("SessionId").AsGuid().NotNullable()
            .WithColumn("Type").AsInt32().NotNullable() // UserTokenType enum
            .WithColumn("Expiry").AsDateTimeOffset().NotNullable()
            .WithColumn("Data").AsString(255).Nullable();

        // Unique Index on TokenHash
        Create.Index("IX_UserTokens_TokenHash")
            .OnTable("UserTokens")
            .OnColumn("TokenHash").Ascending()
            .WithOptions().Unique();

        // Index on SessionId
        Create.Index("IX_UserTokens_SessionId")
            .OnTable("UserTokens")
            .OnColumn("SessionId").Ascending();

        // Composite Index on Type + Expiry
        Create.Index("IX_UserTokens_Type_Expiry")
            .OnTable("UserTokens")
            .OnColumn("Type").Ascending()
            .OnColumn("Expiry").Ascending();
    }

    public override void Down()
    {
        Delete.Table("UserTokens");
    }
}
