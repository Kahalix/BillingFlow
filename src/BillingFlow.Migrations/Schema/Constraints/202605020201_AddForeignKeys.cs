// File: src/BillingFlow.Migrations/Schema/Constraints/202605020201_AddForeignKeys.cs
using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Constraints;

[Migration(202605020201)]
public class AddForeignKeys : Migration
{
    public override void Up()
    {
        // 1. UserTokens -> Users
        Create.ForeignKey("FK_UserTokens_Users_UserId")
            .FromTable("UserTokens").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

    }

    public override void Down()
    {
        // Explicitly drop the constraint before tables are dropped in earlier migrations
        Delete.ForeignKey("FK_UserTokens_Users_UserId").OnTable("UserTokens");
    }
}
