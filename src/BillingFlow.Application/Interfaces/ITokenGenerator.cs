using BillingFlow.Domain.Entities;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Abstraction for generating authentication and security tokens.
/// </summary>
public interface ITokenGenerator
{
    string GenerateJwt(AppUser user, Guid sessionId);
    string GenerateSecureToken();
}
