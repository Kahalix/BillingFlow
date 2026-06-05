// File: src/BillingFlow.Migrations/Schema/Tables/202605020008_CreatePaymentAttemptsTable.cs
using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020008)]
public class CreatePaymentAttemptsTable : Migration
{
    public override void Up()
    {
        Create.Table("PaymentAttempts")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("InvoiceId").AsGuid().NotNullable()
            .WithColumn("Amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("Provider").AsInt32().NotNullable()
            .WithColumn("ProviderReference").AsString(255).Nullable()
            .WithColumn("CheckoutUrl").AsString(2000).Nullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("ErrorMessage").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("ExpiresAt").AsDateTimeOffset().NotNullable();

        // UNIQUE INDEX: Concurrency lock protecting an invoice from double-allocation during initialization
        Execute.Sql("CREATE UNIQUE INDEX IX_PaymentAttempts_InvoiceId_Active ON PaymentAttempts (InvoiceId) WHERE [Status] IN (1, 2)");

        // UNIQUE INDEX: Guarantees provider-specific session reference uniqueness per gateway
        Execute.Sql("CREATE UNIQUE INDEX IX_PaymentAttempts_Provider_Reference ON PaymentAttempts (Provider, ProviderReference) WHERE [ProviderReference] IS NOT NULL");
    }

    public override void Down()
    {
        Delete.Table("PaymentAttempts");
    }
}
