using FluentValidation;

namespace BillingFlow.Application.Features.Invoices.Commands.GenerateInvoice;

public class GenerateInvoiceValidator : AbstractValidator<GenerateInvoiceCommand>
{
    public GenerateInvoiceValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).NotEmpty();
        RuleFor(x => x).Must(x => x.ToDate >= x.FromDate)
            .WithMessage("The end date (ToDate) must be greater than or equal to the start date (FromDate).");
    }
}
