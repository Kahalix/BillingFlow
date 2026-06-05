using System.Threading.RateLimiting;

using BillingFlow.Api.Infrastructure;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Bootstrapper for the API / Presentation layer.
/// Encapsulates the registration of controllers, swagger, exception handling, and rate limiting.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });

        services.AddControllers();
        services.AddEndpointsApiExplorer();

        // Swagger Configuration with JWT Bearer Auth
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "BillingFlow API", Version = "v1" });

            // 1. Define the security scheme (UPDATED for automatic 'Bearer ' prefix)
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Enter your JWT token directly below. Swagger will automatically add the 'Bearer ' prefix.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            // 2. Apply the scheme globally
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    [] // Empty array means no specific OAuth scopes are required
                }
            });
        });

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
