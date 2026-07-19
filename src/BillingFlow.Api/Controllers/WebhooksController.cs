using System.IO;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Payments.Commands.ProcessStripeWebhook;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BillingFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        // SAFEGUARD: Allows the request body to be read multiple times by different middlewares
        HttpContext.Request.EnableBuffering();

        // leaveOpen: true is good practice when working with the underlying request stream
        using var reader = new StreamReader(HttpContext.Request.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync();

        // Reset the stream position just in case someone else needs it later in the pipeline
        HttpContext.Request.Body.Position = 0;

        var signature = HttpContext.Request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrEmpty(signature))
        {
            return BadRequest("Missing Stripe signature header.");
        }

        // Push raw payload to the Application layer for cryptographic validation and processing
        var command = new ProcessStripeWebhookCommand(json, signature);
        await sender.Send(command);

        // Always answer 200 OK to external webhook providers to prevent retry amplification attacks/loops
        return Ok();
    }
}
