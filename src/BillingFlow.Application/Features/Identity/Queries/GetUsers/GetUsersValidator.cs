using FluentValidation;

namespace BillingFlow.Application.Features.Identity.Queries.GetUsers;

public class GetUsersValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersValidator()
    {
        RuleFor(v => v.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(v => v.SearchTerm)
            .MaximumLength(100).WithMessage("Search term is too long.");
    }
}
