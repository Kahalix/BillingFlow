// File: src/BillingFlow.Application/Authorization/CustomClaimTypes.cs
namespace BillingFlow.Application.Authorization;

/// <summary>
/// Defines custom JWT claims used across the application.
/// </summary>
public static class CustomClaimTypes
{
    public const string SessionId = "sessionId";

    // Namespaced to prevent collisions with standard IdP claims
    public const string Permission = "billingflow.permission";
}
