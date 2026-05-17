// File: src/BillingFlow.Application/Features/Identity/Commands/InitiateMyEmailChange/InitiateMyEmailChangeValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.Identity.Commands.InitiateMyEmailChange;

public class InitiateMyEmailChangeValidator : AbstractValidator<InitiateMyEmailChangeCommand>
{
    public InitiateMyEmailChangeValidator()
    {
        RuleFor(v => v.NewEmail)
            .NotEmpty().WithMessage("New email address is required.")
            .EmailAddress().WithMessage("Please provide a valid email address.")
            .MaximumLength(255).WithMessage("Email address cannot exceed 255 characters.");

        RuleFor(v => v.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required to authorize this change.");
    }
}
