// File: src/BillingFlow.Application/Features/Identity/Commands/RegisterUser/RegisterUserValidator.cs
using BillingFlow.Application.Interfaces;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.RegisterUser;

public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator(IApplicationDbContext context)
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.")
            .MustAsync(async (email, cancellation) =>
            {
                if (string.IsNullOrWhiteSpace(email)) return true; // Handled by NotEmpty

                var normalizedEmail = email.Trim().ToLowerInvariant();

                // UX Guard: Checks if the normalized email already exists.
                // The database unique index serves as the final, absolute safeguard.
                return !await context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellation);
            })
            .WithMessage("This email is already registered in the system.");

        RuleFor(v => v.TargetRole)
            .IsInEnum().WithMessage("A valid target role must be provided.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.");

    }
}
