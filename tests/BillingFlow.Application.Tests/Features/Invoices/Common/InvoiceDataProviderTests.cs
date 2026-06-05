using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Invoices.Common;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;

using FluentAssertions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Invoices.Common;

public class InvoiceDataProviderTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly InvoiceDataProvider _provider;

    public InvoiceDataProviderTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _provider = new InvoiceDataProvider(_dbContextMock.Object);
    }

    [Fact]
    public async Task GetInvoiceDetailsAsync_WhenInvoiceDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        _dbContextMock.Setup(c => c.Clients).Returns(new List<Client>().BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.Invoices).Returns(new List<Invoice>().BuildMockDbSet().Object);

        // Act & Assert
        Func<Task> action = async () => await _provider.GetInvoiceDetailsAsync(invoiceId, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Invoice with ID {invoiceId} could not be found.");
    }

    [Fact]
    public async Task GetInvoiceDetailsAsync_WhenInvoiceExists_ShouldReturnCompleteModel()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var clientId = Guid.NewGuid();

        // 1. Setup Domain Entities using Factory
        var client = DomainTestFactory.CreateActiveClient(id: clientId);
        var invoice = DomainTestFactory.CreateUnpaidInvoice(id: invoiceId, clientId: clientId);
        var item = DomainTestFactory.CreateMockInvoiceItem(invoiceId);

        // 2. Setup DbContext Mocks
        _dbContextMock.Setup(c => c.Clients).Returns(new List<Client> { client }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.Invoices).Returns(new List<Invoice> { invoice }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.InvoiceItems).Returns(new List<InvoiceItem> { item }.BuildMockDbSet().Object);

        // Act
        var result = await _provider.GetInvoiceDetailsAsync(invoiceId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(invoiceId);
        result.Client.CompanyName.Should().Be("Test Corp");
        result.Items.Should().HaveCount(1);
        result.Items.First().Description.Should().Be("Consulting");
    }
}
