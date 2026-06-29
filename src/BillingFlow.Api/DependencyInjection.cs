using System.Net;
using System.Threading.RateLimiting;

using BillingFlow.Api.Infrastructure;
using BillingFlow.Application.Interfaces;

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

using StackExchange.Redis;

using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Bootstrapper for the API / Presentation layer.
/// Encapsulates the registration of controllers, swagger, exception handling, and rate limiting.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        // EDGE REVERSE PROXY CONFIGURATION
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Strict security: Clear default loopback-only trusts
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();

            // Configurable ForwardLimit to accurately process the X-Forwarded-For chain.
            // DEV Profile (Local Docker) uses 1 hop (NGINX only).
            // PROD Profile (Azure App Service) uses 2 hops (Azure LB -> NGINX).
            options.ForwardLimit = configuration.GetValue<int>("ReverseProxy:ForwardLimit", 1);

            // Load trusted proxy network boundaries explicitly based on the active profile.
            // PROD: Defaults to Microsoft's recommended RFC 1918 private spaces for Azure App Service environments.
            // DEV: Strictly overrides and narrows trust down to the local Docker bridge network (172.21.0.0/16).
            var trustedNetworks = configuration.GetSection("ReverseProxy:TrustedNetworks").Get<List<TrustedNetworkConfig>>();

            if (trustedNetworks != null)
            {
                foreach (var net in trustedNetworks)
                {
                    if (IPAddress.TryParse(net.Ip, out var ipAddress))
                    {
                        options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ipAddress, net.Prefix));
                    }
                }
            }
        });

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

        // Exception Handling (.NET 8 standard)
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Rate limiting
        services.AddRateLimiter(options =>
        {
            // Dynamically fetch configuration values
            var globalLimit = configuration.GetValue<int>("RateLimiting:GlobalPermitLimit", 100);
            var globalWindow = configuration.GetValue<int>("RateLimiting:GlobalWindowMinutes", 1);

            var loginLimit = configuration.GetValue<int>("RateLimiting:LoginPermitLimit", 5);
            var loginWindow = configuration.GetValue<int>("RateLimiting:LoginWindowMinutes", 1);

            var pwdLimit = configuration.GetValue<int>("RateLimiting:PasswordResetPermitLimit", 3);
            var pwdWindow = configuration.GetValue<int>("RateLimiting:PasswordResetWindowMinutes", 15);

            var emailLimit = configuration.GetValue<int>("RateLimiting:EmailVerificationPermitLimit", 5);
            var emailWindow = configuration.GetValue<int>("RateLimiting:EmailVerificationWindowMinutes", 15);

            // 1. GLOBAL FALLBACK LIMITER (Per-User / Per-IP Partitioning)
            // Evaluated first for every incoming request across the entire API footprint.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                // Inspect JWT claims to extract the unique system User ID
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // Partitioning Strategy: Authenticated sessions are bound strictly by their Identity ID, 
                // preventing bypasses via IP rotation. Anonymous requests fallback safely to the remote IP.
                var partitionKey = !string.IsNullOrEmpty(userId)
                    ? $"user_{userId}"
                    : $"ip_{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, opt =>
                    new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = globalLimit,
                        Window = TimeSpan.FromMinutes(globalWindow),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // Drop immediately to protect application resources
                    });
            });

            // 2. ENDPOINT-SPECIFIC POLICIES (Isolated, Synchronic, Zero-Allocation, Thread-Safe)

            // Login Abuse Protection (IP-based edge rate limiting, supplemented by account lockout)
            options.AddPolicy("LoginPolicy", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"auth_login_ip_{ip}", opt =>
                    new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = loginLimit,
                        Window = TimeSpan.FromMinutes(loginWindow),
                        QueueLimit = 0
                    });
            });

            // Password Reset Spam Protection (Protects external SMTP costs and mail server reputation)
            options.AddPolicy("PasswordResetPolicy", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"auth_reset_ip_{ip}", opt =>
                    new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = pwdLimit,
                        Window = TimeSpan.FromMinutes(pwdWindow),
                        QueueLimit = 0
                    });
            });

            // Email Verification Profiling & Enumeration Protection
            options.AddPolicy("EmailVerificationPolicy", context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter($"auth_verify_ip_{ip}", opt =>
                    new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = emailLimit,
                        Window = TimeSpan.FromMinutes(emailWindow),
                        QueueLimit = 0
                    });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        // 6. OPEN TELEMETRY (Metrics & Distributed Tracing)
        var otlpEndpoint = configuration["Otlp:Endpoint"]
            ?? throw new InvalidOperationException("OTLP Target Collector Endpoint is missing from configuration.");

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("BillingFlow.Api"))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation() // Core HTTP Server Metrics
                .AddHttpClientInstrumentation() // Outbound HTTP Client Metrics
                .AddRuntimeInstrumentation()    // System Core Metrics (CPU, Memory, GC)
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint))) // Pushing Metrics safely via stable OTLP
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation(options =>
                {
                    // Tracks EF Core and Dapper since both use SqlClient internally
                    // db.query.text is emitted automatically by semantic conventions
                    options.RecordException = true; // Records SQL exceptions as trace events
                })
                .AddOtlpExporter(opts => opts.Endpoint = new Uri(otlpEndpoint))); // Pushing Traces safely via stable OTLP

        return services;
    }
}

// Helper class for configuration binding
public class TrustedNetworkConfig
{
    public string Ip { get; set; } = string.Empty;
    public int Prefix { get; set; }
}
