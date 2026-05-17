// File: src/BillingFlow.Application/Features/Identity/Commands/InitiateMyEmailChange/InitiateMyEmailChangeCommand.cs
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateMyEmailChange;

public record InitiateMyEmailChangeCommand(string NewEmail, string CurrentPassword) : IRequest, IAuthenticatedRequest;
