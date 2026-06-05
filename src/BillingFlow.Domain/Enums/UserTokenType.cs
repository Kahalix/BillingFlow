namespace BillingFlow.Domain.Enums;

/// <summary>
/// Defines the types of security tokens issued to a user.
/// </summary>
public enum UserTokenType
{
    RefreshToken = 1,
    PasswordReset = 2,
    EmailConfirmation = 3
}
