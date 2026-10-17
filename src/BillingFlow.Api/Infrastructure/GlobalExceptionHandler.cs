using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Domain.Exceptions;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BillingFlow.Api.Infrastructure;

/// <summary>
/// Catches unhandled systemic exceptions across the application.
/// Provides a safety net for legacy features and core domain invariant violations 
/// that have not yet been migrated to the Result pattern.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = new ProblemDetails { Instance = httpContext.Request.Path };

        switch (exception)
        {

            // Retained for Domain Entities enforcing invariants internally
            case DomainException domainEx:
                problemDetails.Title = "Business Rule Violation";
                problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                problemDetails.Detail = domainEx.Message;
                logger.LogWarning("Domain rule violation caught: {Message}", domainEx.Message);
                break;

            // Retained for database-level unique constraint violations (e.g., EF Core interceptors)
            case UniqueConstraintException:
                problemDetails.Title = "Resource Conflict";
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Detail = "The resource you are trying to create already exists or violates a uniqueness constraint.";
                logger.LogWarning("Unique constraint violation caught.");
                break;

            // Expected infrastructure failures
            case ExternalServiceException externalEx:
                problemDetails.Title = "External Service Failure";
                problemDetails.Status = StatusCodes.Status502BadGateway;
                problemDetails.Detail = "We are temporarily unable to communicate with the payment provider. Please try again later.";
                logger.LogError(externalEx, "Integration failed: {Message}", externalEx.Message);
                break;

            // Security edge failures
            case InvalidWebhookSignatureException signatureEx:
                problemDetails.Title = "Invalid Webhook Signature";
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Detail = "The webhook signature could not be verified. Spoofing attempt blocked.";
                logger.LogWarning(signatureEx, "Webhook signature verification failed.");
                break;

            // True unhandled systemic failures
            default:
                problemDetails.Title = "Internal Server Error";
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Detail = "An unexpected error occurred processing your request.";
                logger.LogError(exception, "Unhandled Exception: {Message}", exception.Message);
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
