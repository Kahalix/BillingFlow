// File: src/BillingFlow.Infrastructure/DependencyInjection.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Infrastructure.Authorization;
using BillingFlow.Infrastructure.BackgroundJobs;
using BillingFlow.Infrastructure.Database;
using BillingFlow.Infrastructure.Database.Interceptors;
using BillingFlow.Infrastructure.Identity;

using Hangfire;
using Hangfire.SqlServer;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // 1. Database & Interceptors

        // Register the interceptor so its dependencies (IPublisher) are resolved by DI
        services.AddScoped<DispatchDomainEventsInterceptor>();
        // services.AddScoped<AuditInterceptor>(); // To be added later

        services.AddDbContext<BillingDbContext>((sp, options) =>
        {
            var domainEventsInterceptor = sp.GetRequiredService<DispatchDomainEventsInterceptor>();
            // var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();

            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(domainEventsInterceptor);
            // .AddInterceptors(auditInterceptor, domainEventsInterceptor);
        });

        // Interface mapping for architecture boundary compliance
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<BillingDbContext>());

        // 2. Identity Services
        services.AddHttpContextAccessor(); // Required to read claims from HTTP request
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddSingleton<ITokenHashService, TokenHashService>();
        services.AddSingleton<IPermissionClaimsProvider, PermissionClaimsProvider>();

        // System time abstraction for unit testing
        services.AddSingleton(TimeProvider.System);

        // 3. External Services (Stripe, Hangfire, etc.)

        // Use ConsoleEmailSender for local development. 
        // For production, a real implementation (e.g., SendGrid/SMTP) should be registered.
        if (environment.IsDevelopment())
        {
            services.AddTransient<IEmailSender, ConsoleEmailSender>();
        }
        else
        {
            // services.AddTransient<IEmailSender, SmtpEmailSender>();
        }

        // services.AddScoped<IStripeService, StripeIntegrationService>();

        // --- Hangfire Configuration ---
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

        // Register the background job processing server (Worker)
        services.AddHangfireServer();

        // Register our custom abstraction to decouple the Application layer from Hangfire
        services.AddTransient<BillingFlow.Application.Interfaces.IBackgroundJobClient, HangfireService>();

        return services;
    }
}
