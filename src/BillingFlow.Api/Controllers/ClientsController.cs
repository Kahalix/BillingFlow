// File: src/BillingFlow.Api/Controllers/ClientsController.cs
using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Features.Clients.Commands.ActivateClient;
using BillingFlow.Application.Features.Clients.Commands.ArchiveClient;
using BillingFlow.Application.Features.Clients.Commands.CreateClient;
using BillingFlow.Application.Features.Clients.Commands.LinkClientUser;
using BillingFlow.Application.Features.Clients.Commands.RestoreClient;
using BillingFlow.Application.Features.Clients.Commands.SuspendClient;
using BillingFlow.Application.Features.Clients.Commands.UnlinkClientUser;
using BillingFlow.Application.Features.Clients.Commands.UpdateClient;
using BillingFlow.Application.Features.Clients.Queries.GetClientDetails;
using BillingFlow.Application.Features.Clients.Queries.GetClients;
using BillingFlow.Application.Features.Clients.Queries.GetMyClientDetails;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Self-Service: Retrieves the client profile linked to the currently authenticated user.
    /// Secure by design (no ID parameter in the route).
    /// </summary>
    [HttpGet("me")]
    [Authorize] // Basic JWT validation required
    [ProducesResponseType(typeof(ClientDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyClientDetails(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyClientDetailsQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a paginated list of all clients in the system.
    /// Can be filtered by status and searched by name or TaxID.
    /// Note: Requesting the 'Archived' status requires Admin or Manager roles.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.ClientsRead)]
    [ProducesResponseType(typeof(PaginatedList<ClientSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetClients(
        [FromQuery] GetClientsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Resource Lookup: Retrieves any client profile by its ID.
    /// Restricted to Back-Office employees with specific permissions.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.ClientsRead)]
    [ProducesResponseType(typeof(ClientDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClientDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetClientDetailsQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new billing client profile.
    /// Restricted to Back-Office employees.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = AppPermissions.ClientsCreate)]
    [ProducesResponseType(typeof(CreateClientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateClient(
        [FromBody] CreateClientCommand command,
        CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);

        // REST Best Practice: Return structured JSON object with the location header
        return CreatedAtAction(nameof(GetClientDetails), new { id = response.ClientId }, response);
    }

    /// <summary>
    /// Updates the core details (name, tax ID, address) of a client profile.
    /// Restricted to Back-Office employees.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = AppPermissions.ClientsUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateClient(
        [FromRoute] Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateClientCommand(
            id,
            request.CompanyName,
            request.TaxId,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country
        );

        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Suspends a client profile, preventing further invoices from being generated.
    /// Restricted to Back-Office employees.
    /// </summary>
    [HttpPut("{id:guid}/suspend")]
    [Authorize(Policy = AppPermissions.ClientsSuspend)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendClient(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SuspendClientCommand(id), cancellationToken);

        // REST Best Practice: Return 204 No Content for a successful update that returns no body.
        return NoContent();
    }

    /// <summary>
    /// Reactivates a suspended client profile, allowing normal pipeline operations.
    /// Restricted to Back-Office employees.
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    [Authorize(Policy = AppPermissions.ClientsActivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateClient(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ActivateClientCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Soft-deletes (archives) a client profile.
    /// Restricted to Admins and Managers.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AppPermissions.ClientsArchive)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveClient(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ArchiveClientCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Restores a previously archived client profile into a Suspended state.
    /// Restricted to Admins and Managers.
    /// </summary>
    [HttpPut("{id:guid}/restore")]
    [Authorize(Policy = AppPermissions.ClientsRestore)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreClient(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new RestoreClientCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Links an AppUser account to a specific billing client profile.
    /// Restricted to Back-Office employees.
    /// </summary>
    [HttpPut("{id:guid}/link-user")]
    [Authorize(Policy = AppPermissions.ClientsUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // When user is already linked
    public async Task<IActionResult> LinkUser(
        [FromRoute] Guid id,
        [FromBody] LinkClientUserRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new LinkClientUserCommand(id, request.UserId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Unlinks the current AppUser account from a billing client profile.
    /// Restricted to Back-Office employees.
    /// </summary>
    [HttpPut("{id:guid}/unlink-user")]
    [Authorize(Policy = AppPermissions.ClientsUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new UnlinkClientUserCommand(id), cancellationToken);
        return NoContent();
    }
}
