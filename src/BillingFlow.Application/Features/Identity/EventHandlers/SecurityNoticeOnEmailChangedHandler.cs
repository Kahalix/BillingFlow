using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Identity.IntegrationEvents;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

namespace BillingFlow.Application.Features.Identity.EventHandlers;

/// <summary>
/// Reacts to an internal domain EmailChangedEvent.
/// Safely delegates the security notice transmission to the Transactional Outbox,
/// ensuring the email is only sent if the underlying database transaction commits successfully.
/// </summary>
public class SecurityNoticeOnEmailChangedHandler(
    IIntegrationEventPublisher eventPublisher) : INotificationHandler<EmailChangedEvent>
{
    public Task Handle(EmailChangedEvent notification, CancellationToken cancellationToken)
    {
        // By publishing an integration event instead of calling Hangfire directly, 
        // we guarantee atomic consistency. If the EF Core transaction fails, 
        // this outbound intent is automatically rolled back.
        eventPublisher.Publish(new SendSecurityNoticeOnEmailChangedEvent(notification.OldEmail));

        return Task.CompletedTask;
    }
}
