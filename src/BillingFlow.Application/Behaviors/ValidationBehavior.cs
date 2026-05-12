// File: src/BillingFlow.Application/Behaviors/ValidationBehavior.cs
using FluentValidation;

using MediatR;

namespace BillingFlow.Application.Behaviors;

/// <summary>
/// Intercepts MediatR requests to perform structural validation
/// using FluentValidation before the request reaches the handler.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        // Execute all validators asynchronously
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Collect all failures
        var failures = validationResults
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            // Throw FluentValidation's native exception containing the exact properties and error codes.
            // The GlobalExceptionHandler will catch this and format it for the client.
            throw new ValidationException(failures);
        }

        return await next(cancellationToken);
    }
}
