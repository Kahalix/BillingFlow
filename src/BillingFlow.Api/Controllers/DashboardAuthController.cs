using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization;
using BillingFlow.Application.Features.Identity.Commands.DashboardLogin;
using BillingFlow.Application.Features.Identity.Commands.DashboardLogout;

using MediatR;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BillingFlow.Api.Controllers;

/// <summary>
/// Dedicated authentication controller for infrastructure dashboards.
/// Protects the boundary between standard API consumers and system administrators.
/// </summary>
[ApiController]
[Route("api/ops/auth")]
[Tags("Infrastructure Ops")]
public class DashboardAuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("DashboardLoginPolicy")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
    [FromBody] DashboardLoginCommand command,
    CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);

        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, response.UserId.ToString()),
        new Claim(ClaimTypes.Role, response.Role.ToString()),
        new Claim(CustomClaimTypes.SessionId, response.SessionId.ToString())
    };

        var identity = new ClaimsIdentity(claims, "AdminCookie");
        await HttpContext.SignInAsync("AdminCookie", new ClaimsPrincipal(identity));

        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "AdminCookie")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await sender.Send(new DashboardLogoutCommand(), cancellationToken);

        await HttpContext.SignOutAsync("AdminCookie");
        return NoContent();
    }
}
