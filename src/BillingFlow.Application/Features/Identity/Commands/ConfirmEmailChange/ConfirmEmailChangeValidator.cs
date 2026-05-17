// File: src/BillingFlow.Application/Features/Identity/Commands/ConfirmEmailChange/ConfirmEmailChangeValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.Identity.Commands.ConfirmEmailChange;

public class ConfirmEmailChangeValidator : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeValidator()
    {
        RuleFor(v => v.Token)
            .NotEmpty().WithMessage("Confirmation token is required.");
    }
}
