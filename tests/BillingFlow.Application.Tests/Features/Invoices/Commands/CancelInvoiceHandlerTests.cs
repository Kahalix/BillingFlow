// File: tests/BillingFlow.Application.Tests/Features/Invoices/Commands/CancelInvoiceHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Invoices.Commands.CancelInvoice;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using FluentAssertions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Invoices.Commands;

public class CancelInvoiceHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly CancelInvoiceHandler _handler;

    public CancelInvoiceHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new CancelInvoiceHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenInvoiceDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var command = new CancelInvoiceCommand(invoiceId);

        // Empty database mockup
        var invoices = new List<Invoice>();
        _dbContextMock.Setup(c => c.Invoices).Returns(invoices.BuildMockDbSet().Object);

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Invoice with ID {invoiceId} could not be found.");

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvoiceExists_ShouldCancelAndSaveChanges()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var command = new CancelInvoiceCommand(invoiceId);

        // Create an invoice and transition it to Unpaid so it can be canceled
        var invoice = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "INV/001", DateTimeOffset.UtcNow);
        invoice.GetType().GetProperty("Id")!.SetValue(invoice, invoiceId);
        invoice.AddLineItem("Test", 100, 1);
        invoice.Issue(); // Status becomes Unpaid

        var invoices = new List<Invoice> { invoice };
        _dbContextMock.Setup(c => c.Invoices).Returns(invoices.BuildMockDbSet().Object);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Canceled);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
