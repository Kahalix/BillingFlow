using System.Reflection;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Behaviors;
using BillingFlow.Application.Features.Clients.Queries.GetClients;
using BillingFlow.Application.Features.Identity.Commands.ActivateUser;
using BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;
using BillingFlow.Application.Features.Identity.Commands.InitiateUserEmailChange;
using BillingFlow.Application.Features.Identity.Commands.RegisterUser;
using BillingFlow.Application.Features.Identity.Commands.SuspendUser;
using BillingFlow.Application.Features.Invoices.Common;
using BillingFlow.Application.Features.Invoices.Queries.DownloadInvoicePdf;
using BillingFlow.Application.Features.Invoices.Queries.GetInvoiceDetails;
using BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;
using BillingFlow.Application.Features.Payments.Queries.GetPaymentDetails;

using FluentValidation;

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

        // 3. Register Specific Authorization Policies
        // (add new policies here as we build the application features)
        // services.AddScoped<IAuthorizationPolicy<GetInvoiceDetailsQuery>, GetInvoiceDetailsPolicy>();

        // Users
        services.AddScoped<IAuthorizationPolicy<RegisterUserCommand>, RegisterUserPolicy>();
        services.AddScoped<IAuthorizationPolicy<ActivateUserCommand>, ActivateUserPolicy>();
        services.AddScoped<IAuthorizationPolicy<ChangeUserRoleCommand>, ChangeUserRolePolicy>();
        services.AddScoped<IAuthorizationPolicy<SuspendUserCommand>, SuspendUserPolicy>();
        services.AddScoped<IAuthorizationPolicy<InitiateUserEmailChangeCommand>, InitiateUserEmailChangePolicy>();

        // Clients
        services.AddScoped<IAuthorizationPolicy<GetClientsQuery>, GetClientsPolicy>();

        // Invoices
        services.AddScoped<IAuthorizationPolicy<GetInvoiceDetailsQuery>, GetInvoiceDetailsPolicy>();
        services.AddScoped<IAuthorizationPolicy<DownloadInvoicePdfQuery>, DownloadInvoicePdfPolicy>();
        
        // Payments
        services.AddScoped<IAuthorizationPolicy<GetPaymentDetailsQuery>, GetPaymentDetailsPolicy>();
        services.AddScoped<IAuthorizationPolicy<CreatePaymentSessionCommand>, CreatePaymentSessionPolicy>();


        // 4. Register Shared Feature Services & Data Providers
        services.AddScoped<IInvoiceDataProvider, InvoiceDataProvider>();

        return services;
    }
}
