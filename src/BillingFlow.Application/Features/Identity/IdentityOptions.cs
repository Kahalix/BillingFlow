using System;

namespace BillingFlow.Application.Identity;

public sealed class IdentityOptions
{
    public const string SectionName = "Identity";

    public TimeSpan SessionLifetime { get; init; }         // Idle timeout (Sliding)
    public TimeSpan AbsoluteSessionLifetime { get; init; } // Hard cap
    public TimeSpan RefreshTokenLifetime { get; init; }
    public TimeSpan PasswordResetLifetime { get; init; }
    public TimeSpan EmailConfirmationLifetime { get; init; }
}
