// File: src/BillingFlow.Infrastructure/DependencyInjection.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Infrastructure.Database;
using BillingFlow.Infrastructure.Database.Interceptors;
using BillingFlow.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
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

        // System time abstraction for unit testing
        services.AddSingleton(TimeProvider.System);

        // 3. External Services (Stripe, Hangfire, etc.)
        // services.AddScoped<IStripeService, StripeIntegrationService>();
        // AddHangfire(...)

        return services;
    }
}
