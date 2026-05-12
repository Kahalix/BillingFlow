// File: src/BillingFlow.Application/Authorization/CustomClaimTypes.cs
namespace BillingFlow.Application.Authorization;

/// <summary>
/// Defines custom claim types used across the application to avoid magic strings.
/// </summary>
public static class CustomClaimTypes
{
    public const string SessionId = "SessionId";
}
