using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020001)]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("Users")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("Email").AsString(255).NotNullable()
            .WithColumn("PasswordHash").AsString(128).NotNullable()
            .WithColumn("Role").AsInt32().NotNullable() // Role enum
            .WithColumn("Status").AsInt32().NotNullable() // UserStatus enum
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("LastLoginAt").AsDateTimeOffset().Nullable()
            .WithColumn("RowVersion").AsCustom("rowversion").NotNullable(); // SQL Server concurrency token

        // Unique index for Email
        Create.Index("IX_Users_Email")
            .OnTable("Users")
            .OnColumn("Email").Ascending()
            .WithOptions().Unique();

        // Composite index for Role + Status
        Create.Index("IX_Users_Role_Status")
            .OnTable("Users")
            .OnColumn("Role").Ascending()
            .OnColumn("Status").Ascending();
    }

    public override void Down()
    {
        // Foreign keys pointing to this table are dropped in the AddForeignKeys migration rollback.
        Delete.Table("Users");
    }
}
