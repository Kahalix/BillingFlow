// File: src/BillingFlow.Migrations/Schema/Seeds/202605020509_SeedInvoices.cs
using System;

using BillingFlow.Domain.Enums;

using FluentMigrator;

namespace BillingFlow.Migrations.Schema.Seeds;

[Migration(202605020509)]
public class SeedInvoices : Migration
{
    private static readonly Guid Invoice1Id = Guid.Parse("00000000-0000-0000-0000-000000000401");
    private static readonly Guid Invoice2Id = Guid.Parse("00000000-0000-0000-0000-000000000402");

    private static readonly Guid Item1Id = Guid.Parse("00000000-0000-0000-0000-000000000501");
    private static readonly Guid Item2Id = Guid.Parse("00000000-0000-0000-0000-000000000502");

    private static readonly Guid Client1Id = Guid.Parse("00000000-0000-0000-0000-000000000201");
    private static readonly Guid Customer1UserId = Guid.Parse("00000000-0000-0000-0000-000000000101");

    public override void Up()
    {
        var issueDate = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        var dueDate = issueDate.AddDays(14);

        Execute.Sql($@"
            -- Invoice 1: Fully Unpaid (Total: 2000.00, Paid: 0.00)
            IF NOT EXISTS (SELECT 1 FROM Invoices WHERE Id = '{Invoice1Id}')
            BEGIN
                INSERT INTO Invoices (Id, ClientId, OwnerUserId, InvoiceNumber, TotalAmount, PaidAmount, IssueDate, DueDate, Status)
                VALUES (
                    '{Invoice1Id}', 
                    '{Client1Id}', 
                    '{Customer1UserId}', 
                    'INV/2026/05/0001', 
                    2000.00, 
                    0.00, 
                    '{issueDate:O}', 
                    '{dueDate:O}', 
                    {(int)InvoiceStatus.Unpaid}
                );

                INSERT INTO InvoiceItems (Id, InvoiceId, Description, UnitPrice, Quantity, LineTotal)
                VALUES ('{Item1Id}', '{Invoice1Id}', 'Software Engineering Services', 1000.00, 2, 2000.00);
            END

            -- Invoice 2: Partially Paid (Total: 1500.00, Paid: 500.00)
            IF NOT EXISTS (SELECT 1 FROM Invoices WHERE Id = '{Invoice2Id}')
            BEGIN
                INSERT INTO Invoices (Id, ClientId, OwnerUserId, InvoiceNumber, TotalAmount, PaidAmount, IssueDate, DueDate, Status)
                VALUES (
                    '{Invoice2Id}', 
                    '{Client1Id}', 
                    '{Customer1UserId}', 
                    'INV/2026/05/0002', 
                    1500.00, 
                    500.00, 
                    '{issueDate:O}', 
                    '{dueDate:O}', 
                    {(int)InvoiceStatus.PartiallyPaid}
                );

                INSERT INTO InvoiceItems (Id, InvoiceId, Description, UnitPrice, Quantity, LineTotal)
                VALUES ('{Item2Id}', '{Invoice2Id}', 'Database Optimization', 1500.00, 1, 1500.00);
            END
        ");
    }

    public override void Down()
    {
        Delete.FromTable("InvoiceItems").Row(new { Id = Item1Id });
        Delete.FromTable("InvoiceItems").Row(new { Id = Item2Id });

        Delete.FromTable("Invoices").Row(new { Id = Invoice1Id });
        Delete.FromTable("Invoices").Row(new { Id = Invoice2Id });
    }
}
