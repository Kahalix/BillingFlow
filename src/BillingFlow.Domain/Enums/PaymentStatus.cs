namespace BillingFlow.Domain.Enums;

/// <summary>
/// Represents the current state of a payment attempt via an external payment gateway.
/// </summary>
public enum PaymentStatus
{
    Initializing = 1, // Phase 1: DB Reservation made, waiting for Gateway URL
    Started = 2,      // Phase 2: Gateway URL acquired, user is paying
    Succeeded = 3,
    Failed = 4,
    Expired = 5
}
