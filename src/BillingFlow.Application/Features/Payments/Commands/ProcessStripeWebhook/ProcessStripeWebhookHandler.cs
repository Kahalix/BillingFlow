using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Features.Payments.Commands.ProcessPaymentCompleted;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.Extensions.Logging;

namespace BillingFlow.Application.Features.Payments.Commands.ProcessStripeWebhook;

/// <summary>
/// Orchestrates external Stripe webhook events, validating their cryptographic signature, 
/// parsing the payload, and translating them into internal domain commands.
/// </summary>
public class ProcessStripeWebhookHandler(
    IStripeWebhookValidator webhookValidator,
    ISender sender) : ICommandHandler<ProcessStripeWebhookCommand>
{
    public async Task<Result> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        // 1. Cryptographic validation and event parsing
        var paymentEvent = webhookValidator.ValidateAndParse(request.JsonPayload, request.StripeSignature);

        // Unhandled valid events or missing metadata. Validator already logged warnings if needed.
        if (paymentEvent == null)
        {
            return Result.Success();
        }

        // 2. Translate external integration event into an internal domain command
        var command = new ProcessPaymentCompletedCommand(
            paymentEvent.EventId,
            paymentEvent.InvoiceId,
            paymentEvent.ProviderReference,
            paymentEvent.Method);

        // 3. Dispatch to the core domain handler
        await sender.Send(command, cancellationToken);

        // 4. Contract requirement: Always ACK to Stripe to prevent retry loops
        return Result.Success();
    }
}
