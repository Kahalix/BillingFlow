// File: tests/BillingFlow.Application.Tests/Features/Invoices/Queries/DownloadInvoicePdfHandlerTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Invoices.Common;
using BillingFlow.Application.Features.Invoices.Queries.DownloadInvoicePdf;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;

using FluentAssertions;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Invoices.Queries;

public class DownloadInvoicePdfHandlerTests
{
    [Fact]
    public async Task Handle_ShouldGeneratePdfAndSanitizeFileName()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var query = new DownloadInvoicePdfQuery(invoiceId);

        // NAPRAWIONE: Użycie właściwej fabryki DTO z warstwy aplikacji
        var model = ApplicationDtoFactory.CreateInvoiceDetailsModel(
            invoiceId,
            invoiceNumber: "INV/2026/05/001");

        var expectedPdfBytes = new byte[] { 1, 2, 3, 4, 5 };

        var dataProviderMock = new Mock<IInvoiceDataProvider>();
        dataProviderMock
            .Setup(p => p.GetInvoiceDetailsAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var rendererMock = new Mock<IInvoicePdfRenderer>();
        rendererMock
            .Setup(r => r.RenderAsync(model, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPdfBytes);

        var handler = new DownloadInvoicePdfHandler(dataProviderMock.Object, rendererMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().BeEquivalentTo(expectedPdfBytes);

        // Ensure slashes are replaced with underscores
        result.FileName.Should().Be("Invoice_INV_2026_05_001.pdf");
    }
}
