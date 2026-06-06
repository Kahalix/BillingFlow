using System.Text;

using BillingFlow.Application.Authorization;
using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;
using BillingFlow.Infrastructure.Authorization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
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

                // Real-time Session Revocation Check
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        // Resolve scoped dependencies from the current HTTP Request context
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
                        var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();

                        var sessionIdClaim = context.Principal?.FindFirst(CustomClaimTypes.SessionId);

                        if (sessionIdClaim == null || !Guid.TryParse(sessionIdClaim.Value, out var sessionId))
                        {
                            context.Fail("Token does not contain a valid SessionId.");
                            return;
                        }

                        var now = timeProvider.GetUtcNow();

                        // Strict Revocation Check: 
                        // The session is only valid if its root RefreshToken is unconsumed and not expired.
                        var isSessionValid = await dbContext.UserTokens
                            .AnyAsync(t =>
                                t.SessionId == sessionId &&
                                t.Type == UserTokenType.RefreshToken &&
                                t.ConsumedAt == null &&
                                t.Expiry > now,
                                context.HttpContext.RequestAborted);

                        if (!isSessionValid)
                        {
                            // Triggers an immediate HTTP 401 Unauthorized, disregarding the JWT's internal validity
                            context.Fail("This session has been revoked or expired.");
                        }
                    }
                };
            });

        // 2. Configure Permission-based Authorization Policies
        // Explicitly mapping each permission guarantees startup-time validation of policy names.
        services.AddAuthorizationBuilder()
            // Users
            .AddPolicy(AppPermissions.UsersRead, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.UsersRead)))
            .AddPolicy(AppPermissions.UsersCreate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.UsersCreate)))
            .AddPolicy(AppPermissions.UsersSuspend, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.UsersSuspend)))
            .AddPolicy(AppPermissions.UsersActivate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.UsersActivate)))
            .AddPolicy(AppPermissions.UsersChangeRole, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.UsersChangeRole)))
            .AddPolicy(AppPermissions.UsersChangeEmail, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.UsersChangeEmail)))

            // Clients
            .AddPolicy(AppPermissions.ClientsRead, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsRead)))
            .AddPolicy(AppPermissions.ClientsCreate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsCreate)))
            .AddPolicy(AppPermissions.ClientsUpdate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsUpdate)))
            .AddPolicy(AppPermissions.ClientsSuspend, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsSuspend)))
            .AddPolicy(AppPermissions.ClientsActivate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsActivate)))
            .AddPolicy(AppPermissions.ClientsArchive, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsArchive)))
            .AddPolicy(AppPermissions.ClientsRestore, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ClientsRestore)))

            // Reports
            .AddPolicy(AppPermissions.ReportsRead, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ReportsRead)))

            // Invoices
            .AddPolicy(AppPermissions.InvoicesRead, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.InvoicesRead)))
            .AddPolicy(AppPermissions.InvoicesGenerate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.InvoicesGenerate)))
            .AddPolicy(AppPermissions.InvoicesCancel, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.InvoicesCancel)))
            
            // ProvidedServices
            .AddPolicy(AppPermissions.ProvidedServicesRead, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ProvidedServicesRead)))
            .AddPolicy(AppPermissions.ProvidedServicesCreate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ProvidedServicesCreate)))
            .AddPolicy(AppPermissions.ProvidedServicesUpdate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ProvidedServicesUpdate)))
            .AddPolicy(AppPermissions.ProvidedServicesDelete, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.ProvidedServicesDelete)))

            // Payments
            .AddPolicy(AppPermissions.PaymentsRead, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.PaymentsRead)))
            .AddPolicy(AppPermissions.PaymentsCreate, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.PaymentsCreate)))
            .AddPolicy(AppPermissions.PaymentsCreateManual, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.PaymentsCreateManual)))
            .AddPolicy(AppPermissions.PaymentsRefund, policy => policy.Requirements.Add(new PermissionRequirement(AppPermissions.PaymentsRefund)));

        // 3. Register the custom permission handler
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
