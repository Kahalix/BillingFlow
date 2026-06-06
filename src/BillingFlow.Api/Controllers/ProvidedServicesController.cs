using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Features.ProvidedServices.Commands.AddProvidedService;
using BillingFlow.Application.Features.ProvidedServices.Commands.CancelProvidedService;
using BillingFlow.Application.Features.ProvidedServices.Commands.UpdateProvidedService;
using BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServiceDetails;
using BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServices;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BillingFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvidedServicesController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retrieves a paginated list of provided services.
    /// Row-Level Security applies dynamically in the handler for Customers.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.ProvidedServicesRead)]
    [ProducesResponseType(typeof(PaginatedList<ProvidedServiceSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProvidedServices(
        [FromQuery] GetProvidedServicesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves full details of a specific provided service.
    /// Access is protected by ABAC: Customers can only view services attached to their client profile.
    /// </summary>
    [HttpGet("{id:guid}", Name = nameof(GetProvidedServiceDetails))]
    [Authorize(Policy = AppPermissions.ProvidedServicesRead)]
    [ProducesResponseType(typeof(ProvidedServiceDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProvidedServiceDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetProvidedServiceDetailsQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates the details of a provided service (e.g., correcting an amount or description).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPermissions.ProvidedServicesUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProvidedService(
        [FromRoute] Guid id,
        [FromBody] UpdateProvidedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProvidedServiceCommand(
            id,
            request.Description,
            request.Amount,
            request.PerformedAt
        );

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-Delete a provided service. Allowed only for draft/unbilled services.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPermissions.ProvidedServicesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProvidedService(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        // Executes the Soft-Delete Domain logic
        await sender.Send(new CancelProvidedServiceCommand(id), cancellationToken);
        return NoContent();
    }
}
