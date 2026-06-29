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
/// Updates the read model projection and dispatches a synchronous best-effort notification.
/// </summary>
public class UpdateClientBalanceOnPaymentRecorded(
    IClientBalanceProjectionWriter projectionWriter,
    IClientNotificationService notificationService,
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

        // 2. BEST-EFFORT NOTIFICATION: Synchronous push to the client.
        // Wrapped in a try-catch to ensure transient network failures in SignalR/Redis
        // do NOT cause a transaction rollback in the database.
        // TODO OUTBOX
        try
        {
            await notificationService.NotifyPaymentRecordedAsync(
                notification.ClientId,
                notification.InvoiceId,
                notification.Amount);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to push real-time notification for Payment to Client {ClientId}. The financial data was projected correctly.",
                notification.ClientId);
        }
    }
}
