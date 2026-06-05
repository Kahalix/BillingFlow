// File: src/BillingFlow.Application/Features/Clients/ReadModels/EventHandlers/UpdateClientBalanceOnPaymentRecorded.cs
using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Clients.ReadModels.EventHandlers;

public class UpdateClientBalanceOnPaymentRecorded(
    IClientBalanceProjectionWriter projectionWriter,
    TimeProvider timeProvider,
    ILogger<UpdateClientBalanceOnPaymentRecorded> logger) : INotificationHandler<PaymentRecordedEvent>
{
    public async Task Handle(PaymentRecordedEvent notification, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        // Negative delta -> Cash received, debt decreases.
        await projectionWriter.ApplyDebtDeltaAsync(notification.ClientId, -notification.Amount, now, cancellationToken);

        logger.LogInformation("Client {ClientId} balance projected. Applied delta: -{Amount}", notification.ClientId, notification.Amount);
    }
}
