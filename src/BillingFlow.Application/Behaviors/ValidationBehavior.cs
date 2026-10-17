using BillingFlow.Application.Common.Results;

using FluentValidation;

using MediatR;

namespace BillingFlow.Application.Behaviors;

/// <summary>
/// Intercepts MediatR requests to perform structural validation.
/// The generic constraint ensures this behavior ONLY applies to requests returning IResult.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult<TResponse> // C# 11 Static Abstract Members Constraint
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count != 0)
        {
            // Maps cleanly to standard RFC 9110 ValidationProblemDetails format.
            // We intentionally drop internal ErrorCodes here to keep user-facing messages clean.
            var errorsDictionary = failures
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray()
                );

            // Compile-time resolution of the factory method. Zero reflection overhead.
            return TResponse.ValidationFailure(errorsDictionary);
        }

        // Explicitly passing the cancellation token to propagate cancellation upstream
        return await next(cancellationToken);
    }
}
