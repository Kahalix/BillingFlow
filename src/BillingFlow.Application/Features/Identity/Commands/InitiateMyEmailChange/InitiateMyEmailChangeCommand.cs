using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateMyEmailChange;

public record InitiateMyEmailChangeCommand(string NewEmail, string CurrentPassword) : ICommand, IAuthenticatedRequest;
