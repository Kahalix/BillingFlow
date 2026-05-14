// File: src/BillingFlow.Application/Interfaces/ITokenHashService.cs
namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Provides deterministic cryptographic hashing for security tokens (e.g., refresh tokens).
/// Allows for O(1) database lookups during token verification.
/// </summary>
public interface ITokenHashService
{
    string HashToken(string rawToken);
}
