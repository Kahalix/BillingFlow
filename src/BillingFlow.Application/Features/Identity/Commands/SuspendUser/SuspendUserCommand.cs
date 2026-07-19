using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.SuspendUser;

public record SuspendUserCommand(Guid UserId) : ICommand, IRequirePermission, IRequirePolicy;
