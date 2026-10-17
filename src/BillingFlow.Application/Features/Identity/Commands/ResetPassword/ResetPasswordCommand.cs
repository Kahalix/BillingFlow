using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.ResetPassword;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmNewPassword) : ICommand, IPublicRequest;
