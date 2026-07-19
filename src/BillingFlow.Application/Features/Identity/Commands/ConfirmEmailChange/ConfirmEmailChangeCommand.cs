using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.ConfirmEmailChange;

public record ConfirmEmailChangeCommand(string Token) : ICommand, IPublicRequest;
