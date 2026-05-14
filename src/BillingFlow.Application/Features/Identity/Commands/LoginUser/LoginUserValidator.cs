// File: src/BillingFlow.Application/Features/Identity/Commands/LoginUser/LoginUserValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.Identity.Commands.LoginUser;

public class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
