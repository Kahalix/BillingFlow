using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.LoginUser;

/// <summary>
/// Command to authenticate a user and issue security tokens.
/// This is a public endpoint and does not require prior authentication.
/// </summary>
public record LoginUserCommand(
    string Email,
    string Password) : ICommand<AuthTokensResponse>, IPublicRequest;
