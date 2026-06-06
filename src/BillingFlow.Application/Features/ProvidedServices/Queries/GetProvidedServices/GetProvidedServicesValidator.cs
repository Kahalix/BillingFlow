using FluentValidation;

namespace BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServices;

public class GetProvidedServicesValidator : AbstractValidator<GetProvidedServicesQuery>
{
    public GetProvidedServicesValidator()
    {
        RuleFor(v => v.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(v => v.SearchTerm)
            .MaximumLength(100).WithMessage("Search term cannot exceed 100 characters.");

        RuleFor(v => v.FilterByClientId)
            .NotEqual(System.Guid.Empty).When(v => v.FilterByClientId.HasValue)
            .WithMessage("Client ID filter must be a valid GUID.");
    }
}
