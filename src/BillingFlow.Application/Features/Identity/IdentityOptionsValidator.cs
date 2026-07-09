using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace BillingFlow.Application.Identity;

public sealed class IdentityOptionsValidator : IValidateOptions<IdentityOptions>
{
    public ValidateOptionsResult Validate(string? name, IdentityOptions options)
    {
        var failures = new List<string>();

        if (options.SessionLifetime <= TimeSpan.Zero)
            failures.Add($"{nameof(options.SessionLifetime)} must be strictly greater than zero.");
        else if (options.SessionLifetime > TimeSpan.FromDays(365))
            failures.Add($"{nameof(options.SessionLifetime)} is unreasonably long.");

        if (options.AbsoluteSessionLifetime <= TimeSpan.Zero)
            failures.Add($"{nameof(options.AbsoluteSessionLifetime)} must be strictly greater than zero.");
        else if (options.AbsoluteSessionLifetime < options.SessionLifetime)
            failures.Add($"{nameof(options.AbsoluteSessionLifetime)} cannot be shorter than {nameof(options.SessionLifetime)}.");

        if (options.RefreshTokenLifetime <= TimeSpan.Zero)
            failures.Add($"{nameof(options.RefreshTokenLifetime)} must be strictly greater than zero.");
        else if (options.RefreshTokenLifetime > TimeSpan.FromDays(365))
            failures.Add($"{nameof(options.RefreshTokenLifetime)} is unreasonably long.");

        if (options.PasswordResetLifetime <= TimeSpan.Zero)
            failures.Add($"{nameof(options.PasswordResetLifetime)} must be strictly greater than zero.");

        if (options.EmailConfirmationLifetime <= TimeSpan.Zero)
            failures.Add($"{nameof(options.EmailConfirmationLifetime)} must be strictly greater than zero.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
