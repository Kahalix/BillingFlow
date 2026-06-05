// File: src/BillingFlow.Api/Controllers/PaymentsController.cs
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Features.Payments.Commands.CreateManualPayment;
using BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;
using BillingFlow.Application.Features.Payments.Queries.GetPaymentDetails;
using BillingFlow.Application.Features.Payments.Queries.GetPayments;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BillingFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController(ISender sender) : ControllerBase
{

    /// <summary>
    /// Retrieves a filtered, paginated list of payments. 
    /// Row-Level Security applies dynamically in the handler to ensure Customers only see their own payments.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.PaymentsRead)]
    [ProducesResponseType(typeof(PaginatedList<PaymentSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] GetPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves detailed information about a specific payment ledger record.
    /// Access is protected by ABAC: Customers can only view payments related to their own invoices.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPaymentDetailsQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Initializes a secure online payment session via Stripe for a specific invoice.
    /// Access is protected by ABAC: Customers can only pay their own invoices.
    /// </summary>
    [HttpPost("session")]
    [Authorize(Policy = AppPermissions.PaymentsCreate)]
    [ProducesResponseType(typeof(PaymentSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreatePaymentSession(
        [FromBody] CreatePaymentSessionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePaymentSessionCommand(
            request.InvoiceId,
            request.SuccessUrl,
            request.CancelUrl);

        var result = await sender.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Registers a manual back-office payment (e.g., Cash, Bank Transfer) directly into the ledger.
    /// Restricted strictly to Back-Office employees.
    /// </summary>
    [HttpPost("manual")]
    [Authorize(Policy = AppPermissions.PaymentsCreateManual)]
    [ProducesResponseType(typeof(PaymentCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateManualPayment(
        [FromBody] CreateManualPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateManualPaymentCommand(
            request.InvoiceId,
            request.Amount,
            request.Method,
            request.PaymentDate,
            request.Notes);

        var paymentId = await sender.Send(command, cancellationToken);

        return CreatedAtAction(
            actionName: nameof(GetPaymentDetails),
            routeValues: new { id = paymentId },
            value: new PaymentCreatedResponse(paymentId));
    }

}
