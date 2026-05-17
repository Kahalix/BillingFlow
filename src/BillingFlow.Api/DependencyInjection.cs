// File: src/BillingFlow.Api/DependencyInjection.cs
using System.Threading.RateLimiting;

using BillingFlow.Api.Infrastructure;

using Microsoft.AspNetCore.RateLimiting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Bootstrapper for the API / Presentation layer.
/// Encapsulates the registration of controllers, swagger, exception handling, and rate limiting.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Modern Exception Handling (.NET 8 standard)
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Security: Rate Limiting
        services.AddRateLimiter(options =>
        {
            // Policy specifically for password resets to prevent brute-force / spam
            options.AddFixedWindowLimiter("PasswordResetPolicy", opt =>
            {
                opt.PermitLimit = 3; // Max 3 requests
                opt.Window = TimeSpan.FromMinutes(15); // per 15 minutes
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0; // Reject immediately, do not queue
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}
