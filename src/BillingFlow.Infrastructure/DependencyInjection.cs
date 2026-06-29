using BillingFlow.Application.Interfaces;
using BillingFlow.Infrastructure.Authorization;
using BillingFlow.Infrastructure.BackgroundJobs;
using BillingFlow.Infrastructure.Database;
using BillingFlow.Infrastructure.Database.Interceptors;
using BillingFlow.Infrastructure.Identity;
using BillingFlow.Infrastructure.Invoices;
using BillingFlow.Infrastructure.Projections;
using BillingFlow.Infrastructure.Stripe;
using BillingFlow.Infrastructure.Outbox;

using Hangfire;
using Hangfire.SqlServer;

using Stripe;
using Stripe.Checkout;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using QuestPDF.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Bootstrapper for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // 1. Database & Interceptors

        // Register the interceptor so its dependencies (IPublisher) are resolved by DI
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
                   .AddInterceptors(auditInterceptor, domainEventsInterceptor);
        });

        // Interface mapping for architecture boundary compliance
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<BillingDbContext>());
        services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();

        services.AddScoped<IClientBalanceProjectionWriter, ClientBalanceProjectionWriter>();

        // 2. Identity Services
        services.AddHttpContextAccessor(); // Required to read claims from HTTP request
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenGenerator, TokenGenerator>();
        services.AddSingleton<ITokenHashService, TokenHashService>();
        services.AddSingleton<IPermissionClaimsProvider, PermissionClaimsProvider>();
        services.AddSingleton<IInvoiceNumberGenerator, SequenceInvoiceNumberGenerator>();

        // System time abstraction for unit testing
        services.AddSingleton(TimeProvider.System);

        // 3. External Services (Stripe, Hangfire, etc.)

        // 1. Configure Global Infrastructure Settings
        QuestPDF.Settings.License = LicenseType.Community;

        // 2. Register Services
        services.AddSingleton<IInvoicePdfRenderer, QuestPdfInvoiceRenderer>();


        // Use ConsoleEmailSender for local development. 
        // For production, a real implementation (e.g., SendGrid/SMTP) should be registered.
        if (environment.IsDevelopment())
        {
            services.AddTransient<IEmailSender, ConsoleEmailSender>();
        }
        else
        {
            //services.AddTransient<IEmailSender, SmtpEmailSender>();
        }

        // Stripe

        // Global key
        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];


        // 1. Register Stripe SessionService
        services.AddTransient<SessionService>();

        // 2. Register our custom StripeService
        services.AddScoped<IStripeService, StripeIntegrationService>();
        services.AddScoped<IStripeWebhookValidator, StripeWebhookValidator>();

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


        // 4. Transactional Outbox Pattern

        // We want IIntegrationEventPublisher to share the exact same Scoped BillingDbContext instance 
        // as the rest of the HTTP request transaction. This guarantees that SaveChangesAsync 
        // commits the outbox message atomically alongside domain changes.
        services.AddScoped<IIntegrationEventPublisher, OutboxEventPublisher>();

        // Transient: The dispatcher is stateless and simply translates and routes payloads to the actual broker.
        services.AddTransient<IIntegrationEventDispatcher, HangfireIntegrationEventDispatcher>();

        return services;
    }
}
