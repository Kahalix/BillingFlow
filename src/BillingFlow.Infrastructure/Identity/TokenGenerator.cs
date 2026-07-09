using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using BillingFlow.Application.Authorization;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Implementation of the token generator.
/// Responsible for creating short-lived JWTs for API access and cryptographically strong, 
/// URL-safe tokens for refresh and reset flows.
/// </summary>
public class TokenGenerator : ITokenGenerator
{
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;
    private readonly IPermissionClaimsProvider _permissionClaimsProvider;
    private readonly SigningCredentials _credentials;

    public TokenGenerator(
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider,
        IPermissionClaimsProvider permissionClaimsProvider)
    {
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
        _permissionClaimsProvider = permissionClaimsProvider;

        // JwtOptions are validated during application startup via IValidateOptions<JwtOptions>.
        // We cache the SigningCredentials here to prevent costly allocations per-request.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// Generates a JSON Web Token (JWT) containing user claims, specific session identifier,
    /// and fine-grained permissions based on the user's role.
    /// </summary>  
    public string GenerateJwt(AppUser user, Guid sessionId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(CustomClaimTypes.SessionId, sessionId.ToString())
        };

        var permissionClaims = _permissionClaimsProvider.GetClaimsForRole(user.Role);
        claims.AddRange(permissionClaims);

        var now = _timeProvider.GetUtcNow();

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),

            Expires = now.AddMinutes(_jwtOptions.AccessTokenLifetimeMinutes).UtcDateTime,
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,

            SigningCredentials = _credentials, // Reusing the cached, thread-safe credentials
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience
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
        var randomNumber = RandomNumberGenerator.GetBytes(32);

        // Produces a URL-safe Base64 string without '+', '/', or '=' padding.
        return WebEncoders.Base64UrlEncode(randomNumber);
    }
}
