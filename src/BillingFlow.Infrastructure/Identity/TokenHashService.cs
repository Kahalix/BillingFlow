// File: src/BillingFlow.Infrastructure/Identity/TokenHashService.cs
using System.Security.Cryptography;
using System.Text;

using BillingFlow.Application.Interfaces;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Implementation of deterministic token hashing using SHA-256.
/// </summary>
public class TokenHashService : ITokenHashService
{
    public string HashToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new ArgumentException("Token cannot be empty.", nameof(rawToken));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));

        // Storing as a lowercase hex string is standard practice for SHA-256 hashes
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
