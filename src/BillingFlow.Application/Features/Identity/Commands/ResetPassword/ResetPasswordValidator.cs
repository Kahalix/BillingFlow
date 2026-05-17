// File: src/BillingFlow.Application/Features/Identity/Commands/ResetPassword/ResetPasswordValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.Identity.Commands.ResetPassword;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().EmailAddress();

        RuleFor(v => v.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(v => v.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        RuleFor(v => v.ConfirmNewPassword)
            .Equal(v => v.NewPassword).WithMessage("Passwords do not match.");
    }
}
