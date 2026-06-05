// File: src/BillingFlow.Domain/Enums/PaymentMethod.cs
namespace BillingFlow.Domain.Enums;

/// <summary>
/// Represents the actual financial instrument or channel used to settle the payment.
/// </summary>
public enum PaymentMethod
{
    Card = 1,
    BankTransfer = 2,
    Cash = 3,
    Blik = 4,
    ManualAdjustment = 5,
    Unknown = 6
}
