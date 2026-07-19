using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.ChangePassword;

/// <summary>
/// Command for a user to change their own password.
/// Requires the user to be currently authenticated.
/// </summary>
public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword) : ICommand, IAuthenticatedRequest;
