using System.Threading.RateLimiting;

using BillingFlow.Api.Infrastructure;
using BillingFlow.Application.Interfaces;

using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Bootstrapper for the API / Presentation layer.
/// Encapsulates the registration of controllers, swagger, exception handling, and rate limiting.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });

        services.AddControllers();
        services.AddEndpointsApiExplorer();

        // 1. SIGNALR REGISTRATION
        // Register the local API implementation for the Hangfire job to resolve
        services.AddTransient<IClientNotificationService, BillingFlow.Api.Services.SignalRClientNotificationService>();

        // Configure SignalR with Redis Backplane for horizontal scaling
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is missing from configuration.");

        services.AddSignalR()
            .AddStackExchangeRedis(redisConnectionString, options =>
            {
                options.Configuration.ChannelPrefix = RedisChannel.Literal("BillingFlow_SignalR_");
            });

        // Swagger Configuration with JWT Bearer Auth
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "BillingFlow API", Version = "v1" });

            // 1. Define the security scheme
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
