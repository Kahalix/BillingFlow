//// File: src/BillingFlow.Infrastructure/DependencyInjection.cs
//using BillingFlow.Application.Interfaces;
//using BillingFlow.Infrastructure.Database;
//using BillingFlow.Infrastructure.Database.Interceptors;
//using BillingFlow.Infrastructure.Identity;

//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;

//namespace Microsoft.Extensions.DependencyInjection;

//public static class DependencyInjection
//{
//    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
//    {
//        // 1. Database & Interceptors
//        services.AddScoped<AuditInterceptor>();
//        services.AddScoped<DispatchDomainEventsInterceptor>();

//        services.AddDbContext<BillingDbContext>((sp, options) =>
//        {
//            var auditInterceptor = sp.GetRequiredService<AuditInterceptor>();
//            var domainEventsInterceptor = sp.GetRequiredService<DispatchDomainEventsInterceptor>();

//            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
//                   .AddInterceptors(auditInterceptor, domainEventsInterceptor);
//        });

//        // Interface mapping
//        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<BillingDbContext>());

//        // 2. Identity Services
//        services.AddHttpContextAccessor(); // Required to read claims from HTTP request
//        services.AddScoped<ICurrentUserService, CurrentUserService>();
//        services.AddSingleton<IPasswordHasher, PasswordHasher>();
//        services.AddSingleton<ITokenGenerator, TokenGenerator>();

//        // 3. External Services (Stripe, Hangfire, etc.)
//        // services.AddScoped<IStripeService, StripeIntegrationService>();
//        // AddHangfire(...)

//        return services;
//    }
//}
