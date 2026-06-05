// File: src/BillingFlow.Application/Features/Payments/Commands/CreatePaymentSession/PaymentSessionResponse.cs
namespace BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;

public record PaymentSessionResponse(
    string ProviderReference,
    string CheckoutUrl
);
