namespace BillingFlow.Domain.Enums;

/// <summary>
/// Encapsulates domain rules and categorizations for payment methods.
/// Prevents Open-Closed Principle (OCP) violations in entities by centralizing method classifications.
/// </summary>
public static class PaymentMethodExtensions
{
    public static bool IsOnlineMethod(this PaymentMethod method)
    {
        // Centralized logic. When adding ApplePay/GooglePay in the future, 
        // you only update this single switch statement.
        return method switch
        {
            PaymentMethod.Card => true,
            PaymentMethod.Blik => true,
            PaymentMethod.Unknown => true, // Fallback for unparsed gateway methods

            PaymentMethod.Cash => false,
            PaymentMethod.BankTransfer => false,
            PaymentMethod.ManualAdjustment => false,

            _ => false
        };
    }

    public static bool IsOfflineMethod(this PaymentMethod method)
    {
        // For declarative use in manual payment creation
        return !method.IsOnlineMethod();
    }
}
