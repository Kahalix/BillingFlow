// File: src/BillingFlow.Application/Features/Identity/Commands/RefreshSession/RefreshSessionValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.Identity.Commands.RefreshSession;

public class RefreshSessionValidator : AbstractValidator<RefreshSessionCommand>
{
    public RefreshSessionValidator()
    {
        RuleFor(v => v.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}
