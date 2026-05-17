// File: src/BillingFlow.Api/Controllers/AuthController.cs
using BillingFlow.Application.Features.Identity.Commands.ConfirmEmailChange;
using BillingFlow.Application.Features.Identity.Commands.LoginUser;
using BillingFlow.Application.Features.Identity.Commands.Logout;
using BillingFlow.Application.Features.Identity.Commands.LogoutAllDevices;
using BillingFlow.Application.Features.Identity.Commands.RefreshSession;
using BillingFlow.Application.Features.Identity.Commands.RegisterUser;
using BillingFlow.Application.Features.Identity.Commands.RequestPasswordReset;
using BillingFlow.Application.Features.Identity.Commands.ResetPassword;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

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

    /// <summary>
    /// Initiates the password recovery process by sending an email with a reset token.
    /// Rate limited to prevent email spam and enumeration attacks.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetPolicy")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] RequestPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        // Zwracamy 202 Accepted, co oznacza "przyjęliśmy żądanie do przetworzenia",
        // co jeszcze mocniej maskuje fakt, czy użytkownik istnieje w bazie.
        return Accepted();
    }

    /// <summary>
    /// Completes the password recovery process using the token sent via email.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Confirms the new email address using the token sent to it.
    /// </summary>
    [HttpPost("confirm-email-change")]
    [AllowAnonymous]
    [EnableRateLimiting("PasswordResetPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmEmailChange(
        [FromBody] ConfirmEmailChangeCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
