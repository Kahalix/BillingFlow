namespace BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;

public record PaymentSessionResponse(
    string ProviderReference,
    string CheckoutUrl
);
