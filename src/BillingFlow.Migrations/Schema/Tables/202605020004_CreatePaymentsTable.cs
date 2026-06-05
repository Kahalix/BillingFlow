// File: src/BillingFlow.Migrations/Schema/Tables/202605020004_CreatePaymentsTable.cs
using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020004)]
public class CreatePaymentsTable : Migration
{
    public override void Up()
    {
        Create.Table("Payments")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("InvoiceId").AsGuid().NotNullable()
            .WithColumn("PaymentAttemptId").AsGuid().Nullable()
            .WithColumn("Amount").AsDecimal(18, 2).NotNullable()
            .WithColumn("PaymentDate").AsDateTimeOffset().NotNullable()
            .WithColumn("Provider").AsInt32().NotNullable()
            .WithColumn("Method").AsInt32().NotNullable()
            .WithColumn("ExternalTransactionId").AsString(255).Nullable()
            .WithColumn("ReceivedByUserId").AsGuid().Nullable()
            .WithColumn("Notes").AsString(1000).Nullable()
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("RowVersion").AsCustom("rowversion").NotNullable();

        // Standard index for fast historical lookups per invoice
        Create.Index("IX_Payments_InvoiceId")
            .OnTable("Payments")
            .OnColumn("InvoiceId").Ascending();

        // UNIQUE INDEX: Prevent processing the same Stripe session twice into the accounting ledger
        Execute.Sql("CREATE UNIQUE INDEX IX_Payments_PaymentAttemptId ON Payments (PaymentAttemptId) WHERE [PaymentAttemptId] IS NOT NULL");

        // UNIQUE INDEX: Global webhook idempotency check based on external provider transaction ID
        Execute.Sql("CREATE UNIQUE INDEX IX_Payments_ExternalTransactionId ON Payments (ExternalTransactionId) WHERE [ExternalTransactionId] IS NOT NULL");
    }

    public override void Down()
    {
        Delete.Table("Payments");
    }
}
