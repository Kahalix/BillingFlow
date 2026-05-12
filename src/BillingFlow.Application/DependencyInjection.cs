// File: src/BillingFlow.Application/DependencyInjection.cs
using System.Reflection;

using BillingFlow.Application.Authorization.Services;
using BillingFlow.Application.Behaviors;

using FluentValidation; // Requires: FluentValidation.DependencyInjectionExtensions

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Bootstrapper for the Application layer.
/// Encapsulates the registration of CQRS handlers, pipeline behaviors, and authorization policies.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 1. Register FluentValidation
        // Scans the assembly and registers all classes inheriting from AbstractValidator<T>
        services.AddValidatorsFromAssembly(assembly);

        // 2. Register MediatR and Pipelines
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // PIPELINE EXECUTION ORDER MATTERS! 

            // 1st: Authorization (block unauthorized access immediately, before allocating memory for validation)
            cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));

            // 2nd: Validation (validate payload structure and business rules only if authorized)
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // 3. Register Authorization Engine
        services.AddScoped<IAuthorizer, Authorizer>();

        // 4. Register Specific Authorization Policies
        // (Uncomment and add new policies here as we build the application features)
        // services.AddScoped<IAuthorizationPolicy<GetInvoiceDetailsQuery>, GetInvoiceDetailsPolicy>();

        return services;
    }
}
