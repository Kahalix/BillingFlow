using System;
using System.Threading.Tasks;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Defines the contract for pushing real-time notifications to connected clients.
/// /// Designed for background execution, hence no CancellationToken is required in the signature.
/// </summary>
public interface IClientNotificationService
{
    /// <summary>
    /// Notifies the client that a payment has been successfully recorded and applied to their balance.
    /// </summary>
    Task NotifyPaymentRecordedAsync(Guid clientId, Guid invoiceId, decimal amount);
}
