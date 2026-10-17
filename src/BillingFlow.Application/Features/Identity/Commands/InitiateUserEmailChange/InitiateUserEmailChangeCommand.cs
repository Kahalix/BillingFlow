using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateUserEmailChange;

// Strictly implements ICommand (which resolves to IRequest<Result>)
public record InitiateUserEmailChangeCommand(Guid TargetUserId, string NewEmail) 
    : ICommand, IRequirePermission, IRequirePolicy;
