// File: src/BillingFlow.Migrations/Programmability/Procedures/202605020404_AddGetMonthlyRevenueProcedure.cs
using FluentMigrator;

namespace BillingFlow.Migrations.Programmability.Procedures;

[Migration(202605020404)]
public class AddGetMonthlyRevenueProcedure : Migration
{
    public override void Up()
    {
        // Snapshot of the enum value at the time this migration was created (May 2026)
        // Prevents historical SQL from silently changing if the Domain Enum is modified in the future.
        const int DraftStatus = 1;

        Execute.Sql($@"
            CREATE OR ALTER PROCEDURE GetMonthlyRevenue
                @StartDate DATETIMEOFFSET,
                @EndDate DATETIMEOFFSET
            AS
            BEGIN
                SET NOCOUNT ON;
                
                SELECT 
                    c.Id AS ClientId,
                    c.CompanyName,
                    c.TaxId,
                    COUNT(i.Id) AS TotalInvoices,
                    ISNULL(SUM(i.TotalAmount), 0) AS TotalBilled,
                    ISNULL(SUM(i.PaidAmount), 0) AS TotalCollected,
                    (ISNULL(SUM(i.TotalAmount), 0) - ISNULL(SUM(i.PaidAmount), 0)) AS OutstandingDebt
                FROM Clients c
                LEFT JOIN Invoices i ON c.Id = i.ClientId 
                    AND i.IssueDate >= @StartDate 
                    AND i.IssueDate < @EndDate 
                    AND i.Status != {DraftStatus}
                GROUP BY 
                    c.Id, 
                    c.CompanyName, 
                    c.TaxId
                ORDER BY 
                    TotalBilled DESC;
            END
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP PROCEDURE IF EXISTS GetMonthlyRevenue;");
    }
}
