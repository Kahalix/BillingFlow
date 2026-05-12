// File: src/BillingFlow.Api/Extensions/AuthorizationExtensions.cs
using System.Text;

using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Infrastructure.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace BillingFlow.Api.Extensions;

/// <summary>
/// Extension methods for setting up authentication and authorization in the API.
/// </summary>
public static class AuthorizationExtensions
{
    public static IServiceCollection AddWebAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Configure JWT Authentication
        var secret = configuration["JwtSettings:Secret"]
            ?? throw new InvalidOperationException("JWT Secret is missing from configuration.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ClockSkew = TimeSpan.Zero // Strictly respect token expiration time
                };
            });

        // 2. Configure Permission-based Authorization Policies
        services.AddAuthorizationBuilder()
            // Clients
            .AddPolicy(AppPermissions.ClientsRead, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsRead)))
            .AddPolicy(AppPermissions.ClientsCreate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsCreate)))
            .AddPolicy(AppPermissions.ClientsSuspend, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsSuspend)))

            // Invoices
            .AddPolicy(AppPermissions.InvoicesRead, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.InvoicesRead)))
            .AddPolicy(AppPermissions.InvoicesGenerate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.InvoicesGenerate)))

            // Payments
            .AddPolicy(AppPermissions.PaymentsRead, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.PaymentsRead)))
            .AddPolicy(AppPermissions.PaymentsRefund, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.PaymentsRefund)));

        // 3. Register the custom permission handler
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
