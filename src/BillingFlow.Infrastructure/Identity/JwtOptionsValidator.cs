using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Options;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Validates the JWT configuration strictly at application startup.
/// Ensures the application fails fast if cryptographic or structural requirements are not met.
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var errors = new List<string>();

        // Secret length and presence validation
        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            errors.Add("JWT Secret is required.");
        }
        else if (Encoding.UTF8.GetByteCount(options.Secret) < 32)
        {
            errors.Add("JWT Secret must contain at least 256 bits (32 bytes) to securely use HS256.");
        }

        // Structural requirements
        if (string.IsNullOrWhiteSpace(options.Issuer))
            errors.Add("JWT Issuer is required.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            errors.Add("JWT Audience is required.");

        if (options.AccessTokenLifetimeMinutes <= 0)
            errors.Add("AccessTokenLifetimeMinutes must be strictly greater than zero.");

        // Aggregate multiple configuration errors for better DevOps UX
        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
