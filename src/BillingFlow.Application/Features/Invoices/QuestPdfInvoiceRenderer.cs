// File: src/BillingFlow.Infrastructure/Invoices/QuestPdfInvoiceRenderer.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Invoices.Common.Models;
using BillingFlow.Application.Interfaces;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BillingFlow.Infrastructure.Invoices;

/// <summary>
/// Infrastructure implementation of the invoice PDF renderer using QuestPDF.
/// Operates as a stateless service. License configuration should be handled at the DI level.
/// </summary>
public class QuestPdfInvoiceRenderer : IInvoicePdfRenderer
{
    public Task<byte[]> RenderAsync(InvoiceDetailsModel invoiceData, CancellationToken cancellationToken)
    {
        // Compose the PDF document structure using QuestPDF Fluent API
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                page.Header().Element(header => ComposeHeader(header, invoiceData));
                page.Content().Element(content => ComposeContent(content, invoiceData));

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        // Generate the binary PDF file in memory
        return Task.FromResult(document.GeneratePdf());
    }

    private void ComposeHeader(IContainer container, InvoiceDetailsModel invoiceData)
    {
        container.Row(row =>
        {
            // Left side: Invoice details
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"INVOICE {invoiceData.InvoiceNumber}")
                    .FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Issue Date: {invoiceData.IssueDate:yyyy-MM-dd}");
                column.Item().Text($"Due Date: {invoiceData.DueDate:yyyy-MM-dd}");
                column.Item().Text($"Status: {invoiceData.Status}").SemiBold();
            });

            // Right side: Client billing information
            row.ConstantItem(150).AlignRight().Column(column =>
            {
                column.Item().Text("Billed To:").SemiBold();
                column.Item().Text(invoiceData.Client.CompanyName);
                column.Item().Text($"Tax ID: {invoiceData.Client.TaxId}");
            });
        });
    }

    private void ComposeContent(IContainer container, InvoiceDetailsModel invoiceData)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(5);

            // Render the line items table
            column.Item().Element(tableContainer => ComposeTable(tableContainer, invoiceData));

            // Render the financial summary at the bottom right
            column.Item().AlignRight().Text($"Total Amount: {invoiceData.TotalAmount:C}")
                .FontSize(14).SemiBold();
            column.Item().AlignRight().Text($"Paid: {invoiceData.PaidAmount:C}");

            var amountDue = invoiceData.TotalAmount - invoiceData.PaidAmount;
            column.Item().AlignRight().Text($"Amount Due: {amountDue:C}")
                .SemiBold().FontColor(Colors.Red.Medium);
        });
    }

    private void ComposeTable(IContainer container, InvoiceDetailsModel invoiceData)
    {
        container.Table(table =>
        {
            // Define column layout
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);  // #
                columns.RelativeColumn();    // Description
                columns.RelativeColumn();    // Unit Price
                columns.RelativeColumn();    // Quantity
                columns.RelativeColumn();    // Total
            });

            // Define table header
            table.Header(header =>
            {
                header.Cell().Text("#").SemiBold();
                header.Cell().Text("Description").SemiBold();
                header.Cell().AlignRight().Text("Unit Price").SemiBold();
                header.Cell().AlignRight().Text("Qty").SemiBold();
                header.Cell().AlignRight().Text("Total").SemiBold();

                header.Cell().ColumnSpan(5).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
            });

            // Populate table rows with line items
            var index = 1;
            foreach (var item in invoiceData.Items)
            {
                table.Cell().Text(index.ToString());
                table.Cell().Text(item.Description);
                table.Cell().AlignRight().Text($"{item.UnitPrice:C}");
                table.Cell().AlignRight().Text(item.Quantity.ToString());
                table.Cell().AlignRight().Text($"{item.LineTotal:C}");
                index++;
            }
        });
    }
}
