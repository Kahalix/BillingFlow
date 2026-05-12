// File: src/BillingFlow.Domain/Enums/PaymentStatus.cs
namespace BillingFlow.Domain.Enums;

/// <summary>
/// Represents the current state of a payment attempt via an external payment gateway.
/// </summary>
public enum PaymentStatus
{
    Started = 1,
    Succeeded = 2,
    Failed = 3
}
