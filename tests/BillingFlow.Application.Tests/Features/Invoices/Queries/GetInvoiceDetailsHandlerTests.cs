using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Invoices.Common;
using BillingFlow.Application.Features.Invoices.Queries.GetInvoiceDetails;
using BillingFlow.Application.Tests.Helpers;

using FluentAssertions;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Invoices.Queries;

public class GetInvoiceDetailsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnModelFromDataProvider()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var query = new GetInvoiceDetailsQuery(invoiceId);

        var expectedModel = ApplicationDtoFactory.CreateInvoiceDetailsModel(invoiceId);

        var dataProviderMock = new Mock<IInvoiceDataProvider>();
        dataProviderMock
            .Setup(p => p.GetInvoiceDetailsAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedModel);

        var handler = new GetInvoiceDetailsHandler(dataProviderMock.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedModel);
        dataProviderMock.Verify(p => p.GetInvoiceDetailsAsync(invoiceId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
