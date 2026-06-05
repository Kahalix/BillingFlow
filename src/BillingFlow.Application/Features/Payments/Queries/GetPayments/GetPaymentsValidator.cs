// File: src/BillingFlow.Application/Features/Payments/Queries/GetPayments/GetPaymentsValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.Payments.Queries.GetPayments;

public class GetPaymentsValidator : AbstractValidator<GetPaymentsQuery>
{
    public GetPaymentsValidator()
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

        RuleFor(v => v.FilterByInvoiceId)
            .NotEqual(System.Guid.Empty).When(v => v.FilterByInvoiceId.HasValue)
            .WithMessage("Invoice ID filter must be a valid GUID.");
    }
}
