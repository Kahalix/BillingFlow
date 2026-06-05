using System;

using BillingFlow.Domain.Enums; // Wymagane dla metody rozszerzającej IsOfflineMethod()

using FluentValidation;

namespace BillingFlow.Application.Features.Payments.Commands.CreateManualPayment;

public class CreateManualPaymentValidator : AbstractValidator<CreateManualPaymentCommand>
{
    public CreateManualPaymentValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.InvoiceId)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Payment amount must be strictly greater than zero.");

        // Prevents querying the database for the invoice if the payload is fundamentally invalid.
        RuleFor(x => x.Method)
            .IsInEnum()
            .Must(m => m.IsOfflineMethod())
            .WithMessage("Manual payments must use an offline payment method (e.g., Cash, BankTransfer, ManualAdjustment).");

        RuleFor(x => x.PaymentDate)
            .NotEmpty()
            .Must(date => date <= timeProvider.GetUtcNow())
            .WithMessage("Payment date cannot be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}
