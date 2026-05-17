// File: src/BillingFlow.Application/Features/Identity/Commands/InitiateUserEmailChange/InitiateUserEmailChangeValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateUserEmailChange;

public class InitiateUserEmailChangeValidator : AbstractValidator<InitiateUserEmailChangeCommand>
{
    public InitiateUserEmailChangeValidator()
    {
        RuleFor(v => v.TargetUserId)
            .NotEmpty().WithMessage("Target User ID is required.");

        RuleFor(v => v.NewEmail)
            .NotEmpty().WithMessage("New email address is required.")
            .EmailAddress().WithMessage("Please provide a valid email address.")
            .MaximumLength(255).WithMessage("Email address cannot exceed 255 characters.");
    }
}
