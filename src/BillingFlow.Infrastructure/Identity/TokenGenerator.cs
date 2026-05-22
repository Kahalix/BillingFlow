// File: src/BillingFlow.Infrastructure/Identity/TokenGenerator.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using BillingFlow.Application.Authorization;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Implementation of the token generator.
/// Responsible for creating short-lived JWTs for API access and cryptographically strong, 
/// URL-safe tokens for refresh and reset flows.
/// </summary>
public class TokenGenerator(
    IConfiguration configuration,
    IPermissionClaimsProvider permissionClaimsProvider)
    : ITokenGenerator
{
    /// <summary>
    /// Generates a JSON Web Token (JWT) containing user claims, specific session identifier,
    /// and fine-grained permissions based on the user's role.
    /// </summary>  
    public string GenerateJwt(AppUser user, Guid sessionId)
    {
        var secret = configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JWT Secret is missing from configuration.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 1. Base identity and session claims
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(CustomClaimTypes.SessionId, sessionId.ToString())
        };

        // 2. Add to token fine-grained static permissions
        var permissionClaims = permissionClaimsProvider.GetClaimsForRole(user.Role);
        claims.AddRange(permissionClaims);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15), // Short-lived access token
            SigningCredentials = credentials,
            Issuer = configuration["JwtSettings:Issuer"],
            Audience = configuration["JwtSettings:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a cryptographically strong, URL-safe random token.
    /// </summary>
    /// <returns>A base64url-encoded string representing 32 bytes of random data.</returns>
    public string GenerateSecureToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        // Produces a URL-safe Base64 string without '+', '/', or '=' padding.
        // This is crucial for embedding tokens safely in query strings (e.g., for email verification or password resets).
        return WebEncoders.Base64UrlEncode(randomNumber);
    }
}
