namespace BillingFlow.Domain.Enums;

/// <summary>
/// Represents the gateway or processor handling the transaction.
/// </summary>
public enum PaymentProvider
{
    BackOffice = 1, // Manual operations by staff
    Stripe = 2,
    //PayPal = 3
}
