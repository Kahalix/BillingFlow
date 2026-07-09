using BillingFlow.Application.Interfaces;
using BillingFlow.Infrastructure.Authorization;
using BillingFlow.Infrastructure.BackgroundJobs;
using BillingFlow.Infrastructure.Database;
using BillingFlow.Infrastructure.Database.Interceptors;
using BillingFlow.Infrastructure.Identity;
using BillingFlow.Infrastructure.Invoices;
using BillingFlow.Infrastructure.Outbox;
using BillingFlow.Infrastructure.Projections;
using BillingFlow.Infrastructure.Stripe;

using Hangfire;
using Hangfire.SqlServer;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
// <-- REQUIRED NAMESPACE FOR REDIS CACHE -->
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using QuestPDF.Infrastructure;

using StackExchange.Redis;

using Stripe;
using Stripe.Checkout;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Bootstrapper for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // 1. Database & Interceptors

        // Register the interceptor so its dependencies (IPublisher, IPostCommitActionQueue) are resolved by DI
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<BillingDbContext>((sp, options) =>
        {
            var domainEventsInterceptor = sp.GetRequiredService<DispatchDomainEventsInterceptor>();
            var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();

            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   // Interceptor order matters:
                   // 1. Dispatch domain events (which might trigger MediatR handlers that mutate DbContext further).
                   // 2. Audit the FINAL state of the ChangeTracker.
                   .AddInterceptors(domainEventsInterceptor, auditInterceptor);
        });

        // Interface mapping for architecture boundary compliance
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<BillingDbContext>());
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IClientBalanceProjectionWriter, ClientBalanceProjectionWriter>();

        // ==============================================================================
        // 2. Identity Services & Session Cache
        // ==============================================================================

        // Options Pattern with strict Fail-Fast validation on application startup
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddOptions<SessionCacheOptions>()
            .Bind(configuration.GetSection(SessionCacheOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<SessionCacheOptions>, SessionCacheOptionsValidator>();

        // --- DISTRIBUTED CACHE & DATA PROTECTION REGISTRATION ---
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // 1. PRIMARY: Redis (Used via Docker Compose and in Production)

            // Create the multiplexer EXACTLY ONCE during startup.
            // This avoids the BuildServiceProvider() anti-pattern and prevents connection leaks.
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);

            redisOptions.AbortOnConnectFail = false;

            var redisMultiplexer = ConnectionMultiplexer.Connect(redisOptions);

            // Register it as a Singleton so other services (e.g., SignalR) can reuse this exact instance.
            services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);

            // Cache (Instructed to reuse the shared multiplexer instead of creating its own)
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redisMultiplexer);
                options.InstanceName = "BillingFlow_Cache:";
            });

            // Data Protection (Reuses the shared multiplexer)
            services.AddDataProtection()
                .SetApplicationName("BillingFlow")
                .PersistKeysToStackExchangeRedis(redisMultiplexer, "DataProtection-Keys");

            // NOTE - XML Encryptor Warning:
            // The code above stores keys in Redis, solving container-restart logout issues.
            // The compiler might still log "No XML encryptor configured". 
            // Inside an isolated Docker bridge network, this is acceptable. 
            // For Production/Cloud (e.g., Azure), uncomment below and use a KeyVault or Certificate:
            // .ProtectKeysWithCertificate(new X509Certificate2("path_to_cert.pfx", "password"));
        }
        else if (environment.IsDevelopment())
        {
            // 2. FALLBACK: In-Memory (ONLY allowed in local development without Docker)

            services.AddDistributedMemoryCache();

            services.AddDataProtection()
                .SetApplicationName("BillingFlow")
                .PersistKeysToFileSystem(new DirectoryInfo("/home/app/.aspnet/DataProtection-Keys"));
        }
        else
        {
            // 3. FAIL-FAST: Production environment strictly requires Redis.
            throw new InvalidOperationException(
                "Redis connection string is missing. A distributed cache and key ring are strictly required in non-development environments to prevent session fragmentation and key loss.");
        }

        services.AddSingleton<ISessionCacheService, DistributedSessionCacheService>();

        // --- AUTH & SECURITY SERVICES ---
        services.AddHttpContextAccessor(); // Required to read claims from HTTP request
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<ISessionValidationService, SessionValidationService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddSingleton<ITokenHashService, TokenHashService>();
        services.AddSingleton<IPermissionClaimsProvider, PermissionClaimsProvider>();
        services.AddSingleton<IInvoiceNumberGenerator, SequenceInvoiceNumberGenerator>();
        services.AddScoped<IRequestContext, HttpRequestContext>();

        // System time abstraction for unit testing
        services.AddSingleton(TimeProvider.System);

        // ==============================================================================
        // 3. External Services (Stripe, Hangfire, etc.)
        // ==============================================================================

        // 1. Configure Global Infrastructure Settings
        QuestPDF.Settings.License = LicenseType.Community;

        // 2. Register Services
        services.AddSingleton<IInvoicePdfRenderer, QuestPdfInvoiceRenderer>();

        // Use ConsoleEmailSender for local development. 
        if (environment.IsDevelopment())
        {
            services.AddTransient<IEmailSender, ConsoleEmailSender>();
        }
        else
        {
            //services.AddTransient<IEmailSender, SmtpEmailSender>();
        }

        // Stripe
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];
        services.AddTransient<SessionService>();
        services.AddScoped<IStripeService, StripeIntegrationService>();
        services.AddScoped<IStripeWebhookValidator, StripeWebhookValidator>();

        // Hangfire
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection not found in configuration.");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
            }));

        services.AddHangfireServer();
        services.AddTransient<BillingFlow.Application.Interfaces.IBackgroundJobClient, HangfireService>();

        // ==============================================================================
        // 4. Transactional Outbox & Post-Commit Patterns
        // ==============================================================================

        // Post-Commit Action Queue (Scoped to current HTTP Request) for UI/UX best-effort notifications (SignalR)
        services.AddScoped<IPostCommitActionQueue, PostCommitActionQueue>();

        // We want IIntegrationEventPublisher to share the exact same Scoped BillingDbContext instance 
        // as the rest of the HTTP request transaction. This guarantees that SaveChangesAsync 
        // commits the outbox message atomically alongside domain changes.
        services.AddScoped<IIntegrationEventPublisher, OutboxEventPublisher>();

        // Transient: The dispatcher is stateless and simply translates and routes payloads to the actual broker.
        services.AddTransient<IIntegrationEventDispatcher, HangfireIntegrationEventDispatcher>();

        return services;
    }
}
