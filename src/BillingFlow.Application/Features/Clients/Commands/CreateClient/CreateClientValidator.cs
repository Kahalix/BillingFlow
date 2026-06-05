using FluentValidation;

namespace BillingFlow.Application.Features.Clients.Commands.CreateClient;

public class CreateClientValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientValidator()
    {
        RuleFor(v => v.UserId)
            .Must(id => !id.HasValue || id.Value != Guid.Empty)
            .WithMessage("User ID cannot be empty.");

        RuleFor(v => v.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters.");

        RuleFor(v => v.TaxId)
            .NotEmpty().WithMessage("Tax ID is required.")
            .MaximumLength(50).WithMessage("Tax ID cannot exceed 50 characters.");

        RuleFor(v => v.Street).NotEmpty().MaximumLength(200);
        RuleFor(v => v.City).NotEmpty().MaximumLength(100);
        RuleFor(v => v.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(v => v.Country).NotEmpty().MaximumLength(100);
    }
}
