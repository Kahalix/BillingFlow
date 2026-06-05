using FluentValidation;

namespace BillingFlow.Application.Features.Identity.Commands.ChangePassword;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(v => v.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(v => v.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.");

        // Ensure the confirmation matches the new password
        RuleFor(v => v.ConfirmNewPassword)
            .Equal(v => v.NewPassword).WithMessage("The new password and confirmation password do not match.");

        // Ensure they aren't setting the same password again
        RuleFor(v => v.NewPassword)
            .NotEqual(v => v.CurrentPassword).WithMessage("New password cannot be the same as the current password.");
    }
}
