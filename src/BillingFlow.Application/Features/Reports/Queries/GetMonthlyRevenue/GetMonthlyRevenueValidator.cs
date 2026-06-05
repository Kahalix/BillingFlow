using FluentValidation;

namespace BillingFlow.Application.Features.Reports.Queries.GetMonthlyRevenue;

/// <summary>
/// Ensures that the incoming query parameters are chronologically valid 
/// before allowing the database to be queried.
/// </summary>
public class GetMonthlyRevenueValidator : AbstractValidator<GetMonthlyRevenueQuery>
{
    public GetMonthlyRevenueValidator()
    {
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be a valid 4-digit year.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12.");
    }
}
