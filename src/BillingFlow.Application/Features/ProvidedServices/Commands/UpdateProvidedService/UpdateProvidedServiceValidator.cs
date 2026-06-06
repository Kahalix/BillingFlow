using FluentValidation;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.UpdateProvidedService;

public class UpdateProvidedServiceValidator : AbstractValidator<UpdateProvidedServiceCommand>
{
    public UpdateProvidedServiceValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PerformedAt).NotEmpty();
    }
}
