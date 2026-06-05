using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

namespace BillingFlow.Application.Features.Identity.EventHandlers;

public class SecurityNoticeOnEmailChangedHandler(IBackgroundJobClient backgroundJobs)
    : INotificationHandler<EmailChangedEvent>
{
    public Task Handle(EmailChangedEvent notification, CancellationToken cancellationToken)
    {
        // Delegate the dispatch of the security notice to the old email address via Hangfire.
        // CancellationToken.None is required here because Hangfire manages cancellation within its own lifecycle.
        backgroundJobs.Enqueue<IEmailSender>(sender =>
            sender.SendEmailChangedNoticeAsync(notification.OldEmail, CancellationToken.None));

        return Task.CompletedTask;
    }
}
