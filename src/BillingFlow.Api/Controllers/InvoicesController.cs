using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Features.Invoices.Commands.CancelInvoice;
using BillingFlow.Application.Features.Invoices.Commands.GenerateInvoice;
using BillingFlow.Application.Features.Invoices.Common.Models;
using BillingFlow.Application.Features.Invoices.Queries.DownloadInvoicePdf;
using BillingFlow.Application.Features.Invoices.Queries.GetInvoiceDetails;
using BillingFlow.Application.Features.Invoices.Queries.GetInvoices;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BillingFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController(ISender sender) : ControllerBase
{

    /// <summary>
    /// Retrieves full details of a specific invoice including line items and client summary.
    /// Access is protected by ABAC: Customers can only view their own invoices.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.InvoicesRead)]
    [ProducesResponseType(typeof(InvoiceDetailsModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvoiceDetailsQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a filtered, paginated list of invoices. 
    /// Row-Level Security applies dynamically in the handler for Customers.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.InvoicesRead)]
    [ProducesResponseType(typeof(PaginatedList<InvoiceSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] GetInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Voids an active invoice, reversing client balances and freeing source provided services.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = AppPermissions.InvoicesCancel)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInvoice(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new CancelInvoiceCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Compiles outstanding billable provided services into a finalized corporate invoice.
    /// Restricted to Admins, Managers, and Accountants.
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Policy = AppPermissions.InvoicesGenerate)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateInvoice(
        [FromBody] GenerateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GenerateInvoiceCommand(
            request.ClientId,
            request.FromDate,
            request.ToDate
        );

        var invoiceId = await sender.Send(command, cancellationToken);

        return Created(string.Empty, new { Id = invoiceId });
    }

    /// <summary>
    /// Downloads the generated invoice document as a PDF.
    /// Access is protected by ABAC: Customers can only download their own invoices.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    [Authorize(Policy = AppPermissions.InvoicesRead)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadInvoicePdf(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DownloadInvoicePdfQuery(id), cancellationToken);

        return File(result.Content, "application/pdf", result.FileName);
    }
}
