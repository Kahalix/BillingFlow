using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Api.Hubs;

/// <summary>
/// A secure SignalR Hub for real-time operational notifications.
/// Strictly enforces JWT Authorization before connection is established.
/// </summary>
[Authorize]
public class ClientBalanceHub(ILogger<ClientBalanceHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userIdString))
        {
            // Add connection to a targeted group based on UserId
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userIdString}");
            logger.LogInformation("SignalR Client Connected: User {UserId}, Connection {ConnectionId}", userIdString, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userIdString))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userIdString}");
            logger.LogInformation("SignalR Client Disconnected: User {UserId}, Connection {ConnectionId}", userIdString, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
