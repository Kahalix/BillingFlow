using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Clients.ReadModels.EventHandlers;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Clients.EventHandlers;

public class UpdateClientBalanceOnPaymentRecordedTests
{
    private readonly Mock<IClientBalanceProjectionWriter> _projectionWriterMock;
    private readonly Mock<IClientNotificationService> _notificationServiceMock;
    private readonly UpdateClientBalanceOnPaymentRecorded _handler;

    public UpdateClientBalanceOnPaymentRecordedTests()
    {
        // MockBehavior.Strict throws an exception if any unconfigured method is called,
        // preventing hidden side-effects in the handler.
        _projectionWriterMock = new Mock<IClientBalanceProjectionWriter>(MockBehavior.Strict);
        _notificationServiceMock = new Mock<IClientNotificationService>(MockBehavior.Strict);

        _handler = new UpdateClientBalanceOnPaymentRecorded(
            _projectionWriterMock.Object,
            _notificationServiceMock.Object,
            TimeProvider.System,
            NullLogger<UpdateClientBalanceOnPaymentRecorded>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldApplyNegativeDeltaBeforePushingNotification()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var amount = 150.00m;

        // Corrected parameter order: PaymentId, InvoiceId, ClientId, Amount
        var notification = new PaymentRecordedEvent(Guid.NewGuid(), invoiceId, clientId, amount);

        // Strict sequence validation. 
        // Ensures database projections happen FIRST, and SignalR notifications happen SECOND.
        var sequence = new MockSequence();

        _projectionWriterMock.InSequence(sequence)
            .Setup(p => p.ApplyDebtDeltaAsync(clientId, -amount, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _notificationServiceMock.InSequence(sequence)
            .Setup(n => n.NotifyPaymentRecordedAsync(clientId, invoiceId, amount))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert - VerifyAll ensures all strict setups in the sequence were executed exactly as defined
        _projectionWriterMock.VerifyAll();
        _notificationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenSignalRFails_ShouldSwallowExceptionToPreventTransactionRollback()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var amount = 100m;
        var notification = new PaymentRecordedEvent(Guid.NewGuid(), invoiceId, clientId, amount);

        // Setup the database call to succeed
        _projectionWriterMock
            .Setup(p => p.ApplyDebtDeltaAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Simulate a transient network failure in the SignalR infrastructure
        _notificationServiceMock
            .Setup(n => n.NotifyPaymentRecordedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>()))
            .ThrowsAsync(new Exception("SignalR connection refused or timed out"));

        // Act
        // We record the exception rather than letting it bubble up
        var exception = await Record.ExceptionAsync(() => _handler.Handle(notification, CancellationToken.None));

        // Assert
        // CRITICAL ARCHITECTURAL GUARANTEE: An operational failure (notification) MUST NOT 
        // bubble up and trigger a database transaction rollback for the financial data.
        Assert.Null(exception);

        _projectionWriterMock.Verify(p =>
            p.ApplyDebtDeltaAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
