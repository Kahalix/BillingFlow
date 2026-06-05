using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.RequestPasswordReset;

/// <summary>
/// Initiates the password recovery process. 
/// Always returns success to prevent user enumeration attacks.
/// </summary>
public record RequestPasswordResetCommand(string Email) : IRequest, IPublicRequest;
