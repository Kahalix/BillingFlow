using FluentMigrator;

namespace BillingFlow.Migrations.Programmability.Sequences;

[Migration(202605020403)]
public class AddInvoiceSequence : Migration
{
    public override void Up()
    {
        // Creates a highly concurrent sequence for invoice numbering.
        // Trade-off explicitly accepted: Prioritizes performance via CACHE over strict gapless numbering 
        // (gaps may occur on transaction rollbacks or server restarts).
        Execute.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'InvoiceNumberSequence')
            BEGIN
                CREATE SEQUENCE InvoiceNumberSequence 
                START WITH 1 
                INCREMENT BY 1 
                CACHE 50;
            END
        ");
    }

    public override void Down()
    {
        Execute.Sql("DROP SEQUENCE IF EXISTS InvoiceNumberSequence;");
    }
}

