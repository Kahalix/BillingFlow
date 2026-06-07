using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Events;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Invoices.EventHandlers;

/// <summary>
/// Reacts to an invoice transitioning to the Overdue status.
/// Dispatches a delayed background job to notify the client via email asynchronously.
/// </summary>
public class InvoiceOverdueEventHandler(
    ILogger<InvoiceOverdueEventHandler> logger,
    IApplicationDbContext context,
    IBackgroundJobClient backgroundJobs) : INotificationHandler<InvoiceOverdueEvent>
{
    public async Task Handle(InvoiceOverdueEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Domain Event Triggered: Invoice {InvoiceId} is overdue.", notification.InvoiceId);

        // Fetch necessary routing details using a single optimized database roundtrip
        var payload = await (
            from i in context.Invoices.AsNoTracking()
            join c in context.Clients.AsNoTracking() on i.ClientId equals c.Id
            join u in context.Users.AsNoTracking() on c.UserId equals u.Id
            where i.Id == notification.InvoiceId && c.UserId != null
            select new
            {
                u.Email,
                c.CompanyName,
                i.InvoiceNumber
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (payload is null)
        {
            logger.LogWarning("Cannot send overdue notice. Client for Invoice {InvoiceId} has no associated AppUser or email.", notification.InvoiceId);
            return;
        }

        // PRAGMATIC TRADE-OFF: Hardcoding a 1-minute delay to mitigate the "publish-before-commit" risk. TODO
        // This ensures the database transaction has successfully committed the 'Overdue' status before the email is dispatched.
        // This is a temporary safeguard and should be replaced by the Transactional Outbox pattern in the future.
        backgroundJobs.Schedule<IEmailSender>(sender =>
            sender.SendInvoiceOverdueNoticeAsync(
                payload.Email,
                payload.CompanyName,
                payload.InvoiceNumber,
                notification.AmountDue,
                default),
            TimeSpan.FromMinutes(1));
    }
}
