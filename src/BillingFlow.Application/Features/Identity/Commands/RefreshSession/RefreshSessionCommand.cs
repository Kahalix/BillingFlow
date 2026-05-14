// File: src/BillingFlow.Application/Features/Identity/Commands/RefreshSession/RefreshSessionCommand.cs
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Features.Identity.Commands.LoginUser;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.RefreshSession;

/// <summary>
/// Command to rotate session tokens. Exchanges a valid refresh token for a new JWT and a new refresh token.
/// </summary>
public record RefreshSessionCommand(
    string RefreshToken) : IRequest<AuthTokensResponse>, IPublicRequest;
