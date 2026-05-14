// File: src/BillingFlow.Application/Features/Identity/Commands/LoginUser/LoginUserCommand.cs
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.LoginUser;

/// <summary>
/// Command to authenticate a user and issue security tokens.
/// This is a public endpoint and does not require prior authentication.
/// </summary>
public record LoginUserCommand(
    string Email,
    string Password) : IRequest<AuthTokensResponse>, IPublicRequest;
