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
    private readonly Mock<IPostCommitActionQueue> _postCommitQueueMock;
    private readonly UpdateClientBalanceOnPaymentRecorded _handler;

    public UpdateClientBalanceOnPaymentRecordedTests()
    {
        // MockBehavior.Strict throws an exception if any unconfigured method is called,
        // preventing hidden side-effects in the handler.
        _projectionWriterMock = new Mock<IClientBalanceProjectionWriter>(MockBehavior.Strict);
        _notificationServiceMock = new Mock<IClientNotificationService>(MockBehavior.Strict);
        _postCommitQueueMock = new Mock<IPostCommitActionQueue>(MockBehavior.Strict);

        _handler = new UpdateClientBalanceOnPaymentRecorded(
            _projectionWriterMock.Object,
            _notificationServiceMock.Object,
            _postCommitQueueMock.Object,
            TimeProvider.System,
            NullLogger<UpdateClientBalanceOnPaymentRecorded>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldApplyNegativeDeltaAndEnqueueNotification()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var amount = 150.00m;

        // PaymentRecordedEvent requires: PaymentId, InvoiceId, ClientId and Amount.
        var notification = new PaymentRecordedEvent(Guid.NewGuid(), invoiceId, clientId, amount);

        // Setup the database call to succeed
        _projectionWriterMock
            .Setup(p => p.ApplyDebtDeltaAsync(clientId, -amount, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Capture the exact lambda passed to the post-commit queue
        Func<CancellationToken, Task>? capturedAction = null;
        _postCommitQueueMock
            .Setup(q => q.Enqueue(It.IsAny<Func<CancellationToken, Task>>()))
            .Callback<Func<CancellationToken, Task>>(action => capturedAction = action);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedAction);

        // Configure the notification service before executing the buffered action.
        _notificationServiceMock
            .Setup(n => n.NotifyPaymentRecordedAsync(clientId, invoiceId, amount))
            .Returns(Task.CompletedTask);

        // Invoke the lambda that was placed in the queue
        await capturedAction!(CancellationToken.None);

        // Verify that the projection was applied and the notification callback was buffered.
        _projectionWriterMock.VerifyAll();
        _postCommitQueueMock.VerifyAll();
        _notificationServiceMock.VerifyAll();
    }

    [Fact]
    public async Task Handle_DelegatesNotificationToPostCommitQueue_PreventingTransactionBlocking()
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

        // Setup the queue to just accept the action
        _postCommitQueueMock
            .Setup(q => q.Enqueue(It.IsAny<Func<CancellationToken, Task>>()));

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        // The handler should enqueue the notification instead of invoking it directly.
        // A direct call would fail because IClientNotificationService is a Strict mock
        // without any configured expectations.
        _projectionWriterMock.VerifyAll();
        _postCommitQueueMock.VerifyAll();

        // Verify that the handler does not invoke the notification service directly.
        _notificationServiceMock.Verify(
            x => x.NotifyPaymentRecordedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<decimal>()),
            Times.Never);
    }
}
