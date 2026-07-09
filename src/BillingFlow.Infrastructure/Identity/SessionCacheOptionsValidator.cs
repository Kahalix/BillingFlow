using System;
using System.Collections.Generic;

using Microsoft.Extensions.Options;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Validates the Session Cache configuration strictly at application startup.
/// Ensures the cache TTL configuration is logically sound.
/// </summary>
public sealed class SessionCacheOptionsValidator : IValidateOptions<SessionCacheOptions>
{
    public ValidateOptionsResult Validate(string? name, SessionCacheOptions options)
    {
        var errors = new List<string>();

        if (options.ValidationTtl <= TimeSpan.Zero)
            errors.Add("ValidationTtl must be strictly greater than zero.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
