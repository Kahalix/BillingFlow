using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Clients.IntegrationEvents;
using BillingFlow.Application.Features.Identity.IntegrationEvents;
using BillingFlow.Application.Interfaces;

namespace BillingFlow.Infrastructure.Outbox;

/// <summary>
/// Routes outbox messages into Hangfire background queues.
/// </summary>
public class HangfireIntegrationEventDispatcher(IBackgroundJobClient hangfireClient) : IIntegrationEventDispatcher
{
    public Task DispatchAsync(string eventType, string payload, CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case nameof(SendClientSuspensionNoticeEvent):
                var data = JsonSerializer.Deserialize<SendClientSuspensionNoticeEvent>(payload);
                if (data != null)
                {
                    // Securely offloads the email transmission to a Hangfire worker
                    hangfireClient.Enqueue<IEmailSender>(sender =>
                        sender.SendClientSuspensionNoticeAsync(data.Email, data.CompanyName, CancellationToken.None));
                }
                break;

            case nameof(SendSecurityNoticeOnEmailChangedEvent):
                var securityData = JsonSerializer.Deserialize<SendSecurityNoticeOnEmailChangedEvent>(payload);
                if (securityData != null)
                {
                    hangfireClient.Enqueue<IEmailSender>(sender =>
                        sender.SendEmailChangedNoticeAsync(securityData.OldEmail, CancellationToken.None));
                }
                break;

            case nameof(SendPasswordResetEmailEvent):
                var resetData = JsonSerializer.Deserialize<SendPasswordResetEmailEvent>(payload);
                if (resetData != null)
                {
                    hangfireClient.Enqueue<IEmailSender>(sender =>
                        sender.SendPasswordResetEmailAsync(resetData.Email, resetData.RawToken, CancellationToken.None));
                }
                break;

            case nameof(SendEmailChangeConfirmationEvent):
                var confirmData = JsonSerializer.Deserialize<SendEmailChangeConfirmationEvent>(payload);
                if (confirmData != null)
                {
                    hangfireClient.Enqueue<IEmailSender>(sender =>
                        sender.SendEmailChangeConfirmationAsync(confirmData.NewEmail, confirmData.RawToken, CancellationToken.None));
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown integration event type: {eventType}");
        }

        return Task.CompletedTask;
    }
}
