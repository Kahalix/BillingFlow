using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Common.Models;
using BillingFlow.Application.Features.Identity.Commands.ActivateUser;
using BillingFlow.Application.Features.Identity.Commands.ChangePassword;
using BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;
using BillingFlow.Application.Features.Identity.Commands.InitiateMyEmailChange;
using BillingFlow.Application.Features.Identity.Commands.InitiateUserEmailChange;
using BillingFlow.Application.Features.Identity.Commands.SuspendUser;
using BillingFlow.Application.Features.Identity.Queries.GetCurrentUser;
using BillingFlow.Application.Features.Identity.Queries.GetUserById;
using BillingFlow.Application.Features.Identity.Queries.GetUsers;
using BillingFlow.Domain.Enums;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillingFlow.Api.Controllers;

/// <summary>
/// Manages user accounts, profiles, and administrative administrative actions.
/// All endpoints in this controller require active authentication.
/// Specific permissions are enforced dynamically by the MediatR pipeline.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UsersController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Retrieves the profile of the currently authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Changes the password for the currently authenticated user.
    /// </summary>
    [HttpPut("me/password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Initiates an email change process for the currently authenticated user.
    /// Requires current password for step-up security.
    /// </summary>
    [HttpPost("me/email")]
    [Authorize]
    [EnableRateLimiting("PasswordResetPolicy")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> InitiateMyEmailChange(
        [FromBody] InitiateMyEmailChangeCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Accepted();
    }

    /// <summary>
    /// Retrieves a paginated and optionally filtered list of system users.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = AppPermissions.UsersRead)]
    [ProducesResponseType(typeof(PaginatedList<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves detailed information about a specific user.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = AppPermissions.UsersRead)]
    [ProducesResponseType(typeof(UserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Suspends a user account, instantly revoking all active sessions.
    /// </summary>
    [HttpPut("{id:guid}/suspend")]
    [Authorize(Policy = AppPermissions.UsersSuspend)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SuspendUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new SuspendUserCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Reactivates a suspended user account.
    /// </summary>
    [HttpPut("{id:guid}/activate")]
    [Authorize(Policy = AppPermissions.UsersActivate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ActivateUserCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Modifies the system role of a target user, subject to hierarchical authorization rules.
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Policy = AppPermissions.UsersChangeRole)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(
        [FromRoute] Guid id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeUserRoleCommand(id, request.NewRole), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Initiates an email change process for a specific user.
    /// Restricted to administrators and authorized employees.
    /// </summary>
    [HttpPost("{id:guid}/email")]
    [Authorize(Policy = AppPermissions.UsersChangeEmail)]
    [EnableRateLimiting("PasswordResetPolicy")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InitiateUserEmailChange(
        [FromRoute] Guid id,
        [FromBody] ChangeUserEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new InitiateUserEmailChangeCommand(id, request.NewEmail);

        await sender.Send(command, cancellationToken);
        return Accepted();
    }
}

/// <summary>
/// DTO representing the request body .
/// </summary>
public record ChangeUserRoleRequest(Role NewRole);
public record ChangeUserEmailRequest(string NewEmail);
