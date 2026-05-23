// File: src/BillingFlow.Application/Features/Clients/Commands/UpdateClient/UpdateClientValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.Clients.Commands.UpdateClient;

public class UpdateClientValidator : AbstractValidator<UpdateClientCommand>
{
    public UpdateClientValidator()
    {
        RuleFor(v => v.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(v => v.TaxId).NotEmpty().MaximumLength(50);
        RuleFor(v => v.Street).NotEmpty().MaximumLength(200);
        RuleFor(v => v.City).NotEmpty().MaximumLength(100);
        RuleFor(v => v.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(v => v.Country).NotEmpty().MaximumLength(100);
    }
}
