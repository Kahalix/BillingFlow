using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Clients.ReadModels.EventHandlers;

/// <summary>
/// Handles the financial impact of a recorded payment.
/// Updates the read model projection and explicitly buffers a real-time notification 
/// to be executed ONLY after the database transaction successfully commits.
/// </summary>
public class UpdateClientBalanceOnPaymentRecorded(
    IClientBalanceProjectionWriter projectionWriter,
    IClientNotificationService notificationService,
    IPostCommitActionQueue postCommitQueue,
    TimeProvider timeProvider,
    ILogger<UpdateClientBalanceOnPaymentRecorded> logger) : INotificationHandler<PaymentRecordedEvent>
{
    public async Task Handle(PaymentRecordedEvent notification, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // 1. CRITICAL OPERATION: Apply the negative delta to decrease the client's debt.
        // This is executed within the EF Core Interceptor lifecycle (pre-commit).
        await projectionWriter.ApplyDebtDeltaAsync(
            notification.ClientId,
            -notification.Amount,
            now,
            cancellationToken);

        logger.LogInformation(
            "Client {ClientId} balance projected. Applied delta: -{Amount}",
            notification.ClientId,
            notification.Amount);

        // 2. BUFFER UX NOTIFICATION (POST-COMMIT PATTERN)
        // Instead of calling SignalR here (which could block the active SQL transaction),
        // we enqueue it. It will be flushed and executed ONLY if SaveChangesAsync succeeds.
        postCommitQueue.Enqueue(ct =>
            notificationService.NotifyPaymentRecordedAsync(
                notification.ClientId,
                notification.InvoiceId,
                notification.Amount));
    }
}
