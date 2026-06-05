using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Tables;

[Migration(202605020003)]
public class CreateInvoicesTable : Migration
{
    public override void Up()
    {
        // 1. Create Invoices Table
        Create.Table("Invoices")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("ClientId").AsGuid().NotNullable()
            .WithColumn("OwnerUserId").AsGuid().Nullable()
            .WithColumn("InvoiceNumber").AsString(50).NotNullable()
            .WithColumn("TotalAmount").AsDecimal(18, 2).NotNullable()
            .WithColumn("PaidAmount").AsDecimal(18, 2).NotNullable()
            .WithColumn("IssueDate").AsDateTimeOffset().NotNullable()
            .WithColumn("DueDate").AsDateTimeOffset().NotNullable()
            .WithColumn("Status").AsInt32().NotNullable()
            .WithColumn("RowVersion").AsCustom("rowversion").NotNullable();

        // Indexes for Invoices
        Create.Index("IX_Invoices_ClientId")
            .OnTable("Invoices")
            .OnColumn("ClientId").Ascending();

        Create.Index("IX_Invoices_OwnerUserId")
            .OnTable("Invoices")
            .OnColumn("OwnerUserId").Ascending();

        Create.Index("IX_Invoices_Status")
            .OnTable("Invoices")
            .OnColumn("Status").Ascending();

        // (Note: The Unique Index for InvoiceNumber is separated into 202605020101_AddInvoiceNumberIndex.cs 

        // 2. Create InvoiceItems Table (Child Entity)
        Create.Table("InvoiceItems")
            .WithColumn("Id").AsGuid().PrimaryKey()
            .WithColumn("InvoiceId").AsGuid().NotNullable()
            .WithColumn("Description").AsString(500).NotNullable()
            .WithColumn("UnitPrice").AsDecimal(18, 2).NotNullable()
            .WithColumn("Quantity").AsInt32().NotNullable()
            .WithColumn("LineTotal").AsDecimal(18, 2).NotNullable();

        // Index for child collection querying
        Create.Index("IX_InvoiceItems_InvoiceId")
            .OnTable("InvoiceItems")
            .OnColumn("InvoiceId").Ascending();
    }

    public override void Down()
    {
        Delete.Table("InvoiceItems");
        Delete.Table("Invoices");
    }
}
