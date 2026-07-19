using BillingFlow.Application.Common.Results;

using Microsoft.AspNetCore.Mvc;

using Serilog;

namespace BillingFlow.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Handles Result<T> with a custom success delegate. 
    /// Useful for CreatedAtAction or custom object mapping.
    /// </summary>
    protected IActionResult HandleResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        return result.IsSuccess ? onSuccess(result.Value) : CreateProblemDetails(result.Error);
    }

    /// <summary>
    /// Handles void Result with a custom success delegate.
    /// Useful for returning Accepted() or other specific status codes without payload.
    /// </summary>
    protected IActionResult HandleResult(Result result, Func<IActionResult> onSuccess)
    {
        return result.IsSuccess ? onSuccess() : CreateProblemDetails(result.Error);
    }

    /// <summary>
    /// Default handler for Result<T>. Returns 200 OK with the payload on success.
    /// </summary>
    protected IActionResult HandleResult<T>(Result<T> result)
    {
        return HandleResult(result, value => Ok(value));
    }

    /// <summary>
    /// Default handler for void Result. Returns 204 No Content on success.
    /// </summary>
    protected IActionResult HandleResult(Result result)
    {
        return HandleResult(result, () => NoContent());
    }

    private IActionResult CreateProblemDetails(Error error)
    {
        // FAIL-FAST: GetRequiredService guarantees that IDiagnosticContext exists.
        // If Serilog is misconfigured, the app will throw immediately rather than silently failing to log.
        var diagnosticContext = HttpContext.RequestServices.GetRequiredService<IDiagnosticContext>();
        diagnosticContext.Set("ErrorCode", error.Code);

        if (error is ValidationError validationError)
        {
            return BadRequest(new HttpValidationProblemDetails(validationError.Errors)
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = validationError.Description,
                Instance = HttpContext.Request.Path
            });
        }

        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.UnprocessableEntity => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        var title = error.Type switch
        {
            ErrorType.NotFound => "Resource Not Found",
            ErrorType.Conflict => "Resource Conflict",
            ErrorType.Unauthorized => "Unauthorized",
            ErrorType.Forbidden => "Forbidden",
            ErrorType.UnprocessableEntity => "Unprocessable Entity",
            _ => "Bad Request"
        };

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Status = statusCode,
            Detail = error.Description,
            Instance = HttpContext.Request.Path,
            Extensions = { { "errors", new[] { new { error.Code, error.Description } } } }
        };

        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
}
