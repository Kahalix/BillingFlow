// File: src/BillingFlow.Api/Controllers/AuthController.cs
using BillingFlow.Application.Features.Identity.Commands.LoginUser;
using BillingFlow.Application.Features.Identity.Commands.Logout;
using BillingFlow.Application.Features.Identity.Commands.LogoutAllDevices;
using BillingFlow.Application.Features.Identity.Commands.RefreshSession;
using BillingFlow.Application.Features.Identity.Commands.RegisterUser;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingFlow.Api.Controllers;

/// <summary>
/// Manages authentication, token issuance, and user provisioning.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Provisions a new system user. 
    /// Requires active authentication and the 'users.create' permission.
    /// </summary>
    [HttpPost("register")]
    [Authorize]
    [ProducesResponseType(typeof(RegisterUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var userId = await sender.Send(command, cancellationToken);

        // Return 201 Created with a strictly typed DTO for OpenAPI schema correctness.
        return Created(string.Empty, new RegisterUserResponse(userId));
    }

    /// <summary>
    /// Authenticates a user and issues a short-lived JWT and a long-lived Refresh Token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken cancellationToken)
    {
        var tokens = await sender.Send(command, cancellationToken);
        return Ok(tokens);
    }

    /// <summary>
    /// Rotates session tokens to maintain access without requiring the user to re-enter credentials.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthTokensResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        var tokens = await sender.Send(command, cancellationToken);
        return Ok(tokens);
    }

    /// <summary>
    /// Terminates the current active session by revoking its associated refresh tokens.
    /// Requires an active Bearer token to identify the session.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Terminates all active sessions for the current user across all devices.
    /// Highly recommended after a security breach or losing a device.
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAllDevices(CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutAllDevicesCommand(), cancellationToken);
        return NoContent();
    }
}
