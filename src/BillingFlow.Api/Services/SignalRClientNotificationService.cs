using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Api.Hubs;
using BillingFlow.Application.Interfaces;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Api.Services;

/// <summary>
/// SignalR implementation of the client notification service.
/// Resolves the infrastructure mapping between a billing ClientId and an identity UserId.
/// </summary>
public class SignalRClientNotificationService(
    IHubContext<ClientBalanceHub> hubContext,
    IApplicationDbContext dbContext,
    ILogger<SignalRClientNotificationService> logger) : IClientNotificationService
{
    public async Task NotifyPaymentRecordedAsync(Guid clientId, Guid invoiceId, decimal amount)
    {
        // Execute a fast, filter-ignoring query to resolve the exact owner of the billing profile.
        // CancellationToken.None is used because this runs as a fire-and-forget/best-effort operation.
        var ownerUserId = await dbContext.Clients
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.Id == clientId)
            .Select(c => c.UserId)
            .SingleOrDefaultAsync(CancellationToken.None);

        if (ownerUserId is null)
        {
            logger.LogWarning("Cannot push real-time notification for Client {ClientId}. No linked User found.", clientId);
            return;
        }

        var groupName = $"User_{ownerUserId}";
        var eventPayload = new
        {
            InvoiceId = invoiceId,
            Amount = amount,
            Message = $"A payment of {amount:C} has been successfully applied to your account.",
            Timestamp = DateTimeOffset.UtcNow
        };

        // Broadcast the message strictly to the authenticated user's private group
        await hubContext.Clients.Group(groupName).SendAsync("PaymentRecorded", eventPayload, CancellationToken.None);

        logger.LogInformation("Successfully pushed 'PaymentRecorded' real-time notification to group {GroupName}", groupName);
    }
}
