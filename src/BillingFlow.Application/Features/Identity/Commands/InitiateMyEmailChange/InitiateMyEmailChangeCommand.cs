using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateMyEmailChange;

public record InitiateMyEmailChangeCommand(string NewEmail, string CurrentPassword) : IRequest, IAuthenticatedRequest;
