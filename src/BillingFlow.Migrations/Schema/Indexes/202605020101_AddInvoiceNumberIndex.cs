// File: src/BillingFlow.Migrations/Schema/Indexes/202605020101_AddInvoiceNumberIndex.cs
using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Indexes;

[Migration(202605020101)]
public class AddInvoiceNumberIndex : Migration
{
    public override void Up()
    {
        Create.Index("IX_Invoices_InvoiceNumber")
            .OnTable("Invoices")
            .OnColumn("InvoiceNumber").Ascending()
            .WithOptions().Unique();
    }

    public override void Down()
    {
        Delete.Index("IX_Invoices_InvoiceNumber").OnTable("Invoices");
    }
}
