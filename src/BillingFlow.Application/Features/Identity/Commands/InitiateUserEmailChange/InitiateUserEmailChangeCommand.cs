// File: src/BillingFlow.Application/Features/Identity/Commands/InitiateUserEmailChange/InitiateUserEmailChangeCommand.cs
using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateUserEmailChange;

public record InitiateUserEmailChangeCommand(Guid TargetUserId, string NewEmail) : IRequest, IRequirePermission, IRequirePolicy;
