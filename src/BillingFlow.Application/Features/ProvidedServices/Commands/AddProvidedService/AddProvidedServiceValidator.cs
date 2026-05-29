// File: src/BillingFlow.Application/Features/ProvidedServices/Commands/AddProvidedService/AddProvidedServiceValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.AddProvidedService;

public class AddProvidedServiceValidator : AbstractValidator<AddProvidedServiceCommand>
{
    public AddProvidedServiceValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PerformedAt).NotEmpty();
    }
}
