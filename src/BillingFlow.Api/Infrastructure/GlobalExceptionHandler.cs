// File: src/BillingFlow.Api/Infrastructure/GlobalExceptionHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Domain.Exceptions;

using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BillingFlow.Api.Infrastructure;

/// <summary>
/// Catches unhandled exceptions across the application and maps them to standardized ProblemDetails responses.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        // 1. Handle Validation Errors specifically using HttpValidationProblemDetails
        if (exception is ValidationException validationEx)
        {
            var validationErrors = validationEx.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray()
                );

            var validationProblem = new HttpValidationProblemDetails(validationErrors)
            {
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Validation Failed",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = validationProblem.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken);

            return true;
        }

        // 2. Handle other Business/Domain/System Exceptions
        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        switch (exception)
        {
            case ForbiddenException forbiddenEx:
                problemDetails.Title = "Forbidden";
                problemDetails.Status = StatusCodes.Status403Forbidden;
                problemDetails.Detail = forbiddenEx.Message;
                break;

            case UnauthorizedException unauthEx:
                problemDetails.Title = "Unauthorized";
                problemDetails.Status = StatusCodes.Status401Unauthorized;
                problemDetails.Detail = unauthEx.Message;
                break;

            case NotFoundException notFoundEx:
                problemDetails.Title = "Resource Not Found";
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Detail = notFoundEx.Message;
                break;

            case DomainException domainEx:
                // Domain rules are typically 422 Unprocessable Entity or 409 Conflict
                problemDetails.Title = "Business Rule Violation";
                problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                problemDetails.Detail = domainEx.Message;
                break;

            case UniqueConstraintException:
                // Standard HTTP status for duplicate resources
                problemDetails.Title = "Resource Conflict";
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Detail = "The resource you are trying to create already exists or violates a uniqueness constraint.";
                break;

            case InvalidWebhookSignatureException:
                // A bad signature is a client error / unauthorized attempt
                problemDetails.Title = "Invalid Webhook Signature";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = "The webhook signature could not be verified. Spoofing attempt blocked.";
                break;

            case ExternalServiceException externalEx:
                problemDetails.Title = "External Service Failure";
                problemDetails.Status = StatusCodes.Status502BadGateway;
                problemDetails.Detail = "We are temporarily unable to communicate with the payment provider. Please try again later.";
                // log internal details, but return safe message to client
                logger.LogError(externalEx, "Integration failed: {Message}", externalEx.Message);
                break;

            default:
                problemDetails.Title = "Internal Server Error";
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Detail = "An unexpected error occurred.";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
