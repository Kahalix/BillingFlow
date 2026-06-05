// File: src/BillingFlow.Migrations/Schema/Constraints/202605020201_AddForeignKeys.cs
using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Constraints;

[Migration(202605020201)]
public class AddForeignKeys : Migration
{
    public override void Up()
    {
        // 1. UserTokens -> Users (Technical data - Cascade is fine)
        Create.ForeignKey("FK_UserTokens_Users_UserId")
            .FromTable("UserTokens").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // 2. Clients -> Users (Soft link, if user is deleted, client becomes orphaned but kept for billing history)
        Create.ForeignKey("FK_Clients_Users_UserId")
            .FromTable("Clients").ForeignColumn("UserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.SetNull);

        // 3. Invoices -> Clients
        // ENTERPRISE RULE: Rule.None. Hard deleting a client must be blocked by the DB to preserve financial audit trails.
        Create.ForeignKey("FK_Invoices_Clients_ClientId")
            .FromTable("Invoices").ForeignColumn("ClientId")
            .ToTable("Clients").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        // 4. InvoiceItems -> Invoices
        // ENTERPRISE RULE: Rule.None. Invoices are historical documents; items cannot be cascade-deleted.
        Create.ForeignKey("FK_InvoiceItems_Invoices_InvoiceId")
            .FromTable("InvoiceItems").ForeignColumn("InvoiceId")
            .ToTable("Invoices").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        // 5. ProvidedServices -> Clients
        // ENTERPRISE RULE: Rule.None. Unbilled services are still financial liabilities/assets.
        Create.ForeignKey("FK_ProvidedServices_Clients_ClientId")
            .FromTable("ProvidedServices").ForeignColumn("ClientId")
            .ToTable("Clients").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        // 6. ProvidedServices -> Invoices
        Create.ForeignKey("FK_ProvidedServices_Invoices_InvoiceId")
            .FromTable("ProvidedServices").ForeignColumn("InvoiceId")
            .ToTable("Invoices").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        // 7. ClientBalances (Read Model) -> Clients
        // Cascade is acceptable here because this is just a materialized projection that can be rebuilt if needed.
        Create.ForeignKey("FK_ClientBalances_Clients_ClientId")
            .FromTable("ClientBalances").ForeignColumn("ClientId")
            .ToTable("Clients").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.Cascade);

        // 8. PaymentAttempts -> Invoices
        Create.ForeignKey("FK_PaymentAttempts_Invoices_InvoiceId")
            .FromTable("PaymentAttempts").ForeignColumn("InvoiceId")
            .ToTable("Invoices").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        // 9. Payments -> Invoices
        Create.ForeignKey("FK_Payments_Invoices_InvoiceId")
            .FromTable("Payments").ForeignColumn("InvoiceId")
            .ToTable("Invoices").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        // 10. Payments -> PaymentAttempts
        Create.ForeignKey("FK_Payments_PaymentAttempts_PaymentAttemptId")
            .FromTable("Payments").ForeignColumn("PaymentAttemptId")
            .ToTable("PaymentAttempts").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.None);

        // 11. Payments -> Users (ReceivedByUserId for manual back-office payments)
        // If an employee is deleted, we just set it to NULL, the payment record itself MUST stay.
        Create.ForeignKey("FK_Payments_Users_ReceivedByUserId")
            .FromTable("Payments").ForeignColumn("ReceivedByUserId")
            .ToTable("Users").PrimaryColumn("Id")
            .OnDelete(System.Data.Rule.SetNull);
    }

    public override void Down()
    {
        // Explicitly drop constraints in reverse order
        Delete.ForeignKey("FK_Payments_Users_ReceivedByUserId").OnTable("Payments");
        Delete.ForeignKey("FK_Payments_PaymentAttempts_PaymentAttemptId").OnTable("Payments");
        Delete.ForeignKey("FK_Payments_Invoices_InvoiceId").OnTable("Payments");
        Delete.ForeignKey("FK_PaymentAttempts_Invoices_InvoiceId").OnTable("PaymentAttempts");

        Delete.ForeignKey("FK_ClientBalances_Clients_ClientId").OnTable("ClientBalances");
        Delete.ForeignKey("FK_ProvidedServices_Invoices_InvoiceId").OnTable("ProvidedServices");
        Delete.ForeignKey("FK_ProvidedServices_Clients_ClientId").OnTable("ProvidedServices");
        Delete.ForeignKey("FK_InvoiceItems_Invoices_InvoiceId").OnTable("InvoiceItems");
        Delete.ForeignKey("FK_Invoices_Clients_ClientId").OnTable("Invoices");
        Delete.ForeignKey("FK_Clients_Users_UserId").OnTable("Clients");
        Delete.ForeignKey("FK_UserTokens_Users_UserId").OnTable("UserTokens");
    }
}
