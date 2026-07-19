using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Features.Identity.Commands.LoginUser;

namespace BillingFlow.Application.Features.Identity.Commands.RefreshSession;

/// <summary>
/// Command to rotate session tokens. Exchanges a valid refresh token for a new JWT and a new refresh token.
/// </summary>
public record RefreshSessionCommand(
    string RefreshToken) : ICommand<AuthTokensResponse>, IPublicRequest;
