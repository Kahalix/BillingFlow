using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.ConfirmEmailChange;

public record ConfirmEmailChangeCommand(string Token) : IRequest, IPublicRequest;
