// File: src/BillingFlow.Application/Features/Identity/Commands/LoginUser/AuthTokensResponse.cs
namespace BillingFlow.Application.Features.Identity.Commands.LoginUser;

/// <summary>
/// Contains the authentication payload returned to the client upon successful login.
/// </summary>
public record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);
