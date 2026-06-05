using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Payments.Commands.ProcessPaymentCompleted;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Payments.Commands;

public class ProcessPaymentCompletedHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly ProcessPaymentCompletedHandler _handler;

    public ProcessPaymentCompletedHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();

        _handler = new ProcessPaymentCompletedHandler(
            _dbContextMock.Object,
            TimeProvider.System,
            NullLogger<ProcessPaymentCompletedHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenAttemptIsAlreadySucceeded_ShouldReturnWithoutSaving()
    {
        // Arrange
        var command = new ProcessPaymentCompletedCommand("evt_123", Guid.NewGuid(), "cs_123", PaymentMethod.Card);

        var attempt = PaymentAttempt.Reserve(command.InvoiceId, 100m, PaymentProvider.Stripe, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        attempt.SetCheckoutDetails("cs_123", "url");
        attempt.MarkAsSucceeded(); // ALREADY SUCCEEDED

        _dbContextMock.Setup(c => c.PaymentAttempts).Returns(new List<PaymentAttempt> { attempt }.BuildMockDbSet().Object);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(c => c.Invoices, Times.Never); // Fast pass, invoice shouldn't be queried
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDatabaseThrowsUniqueConstraintOnStripeEventLog_ShouldSwallowExceptionGracefully()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var command = new ProcessPaymentCompletedCommand("evt_123", invoiceId, "cs_123", PaymentMethod.Card);

        var attempt = PaymentAttempt.Reserve(invoiceId, 100m, PaymentProvider.Stripe, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        attempt.SetCheckoutDetails("cs_123", "url");

        var invoice = DomainTestFactory.CreateUnpaidInvoice(invoiceId);

        _dbContextMock.Setup(c => c.PaymentAttempts).Returns(new List<PaymentAttempt> { attempt }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.Invoices).Returns(new List<Invoice> { invoice }.BuildMockDbSet().Object);

        _dbContextMock.Setup(c => c.Payments).Returns(new List<Payment>().BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.StripeEventLogs).Returns(new List<StripeEventLog>().BuildMockDbSet().Object);

        // MOCK RACE CONDITION: SaveChangesAsync throws UniqueConstraintException specifically for StripeEventLog
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintException("Conflict", nameof(StripeEventLog), new Exception()));

        // Act
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await action.Should().NotThrowAsync(); // Exception should be swallowed
    }
}
