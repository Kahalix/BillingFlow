// File: src/BillingFlow.Application/Features/Payments/Commands/CreatePaymentSession/CreatePaymentSessionValidator.cs
using FluentValidation;

namespace BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;

public class CreatePaymentSessionValidator : AbstractValidator<CreatePaymentSessionCommand>
{
    public CreatePaymentSessionValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.SuccessUrl).NotEmpty().Must(uri => System.Uri.TryCreate(uri, System.UriKind.Absolute, out _)).WithMessage("Success URL must be a valid absolute URI.");
        RuleFor(x => x.CancelUrl).NotEmpty().Must(uri => System.Uri.TryCreate(uri, System.UriKind.Absolute, out _)).WithMessage("Cancel URL must be a valid absolute URI.");
    }
}
