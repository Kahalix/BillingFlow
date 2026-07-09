using System;
using System.Text;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization;
using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;
using BillingFlow.Infrastructure.Authorization;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT Secret is missing from configuration.");

        // 1. SMART ROUTER (Policy Scheme)
        // Dynamically forwards authentication resolution based on the request URL.
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "SmartRouter";
            options.DefaultChallengeScheme = "SmartRouter";
        })
        .AddPolicyScheme("SmartRouter", "JWT or Cookie Routing", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var path = context.Request.Path;

                // Infrastructure dashboards and ops logins use browser-friendly cookies
                if (path.StartsWithSegments("/hangfire") || path.StartsWithSegments("/api/ops/auth"))
                {
                    return "AdminCookie";
                }

                // Core application APIs expect strict JWT Bearers
                return JwtBearerDefaults.AuthenticationScheme;
            };
        })

        // 2. JWT BEARER (For Standard API & SignalR)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero // Strictly respect token expiration time
            };

            options.Events = new JwtBearerEvents
            {
                // --- SignalR JWT Configuration ---
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    // Automatically bind the token if the request targets a SignalR hub
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },

                // --- Real-time Session Revocation Check (API) ---
                OnTokenValidated = async context =>
                {
                    var sessionIdClaim = context.Principal?.FindFirst(CustomClaimTypes.SessionId);

                    if (sessionIdClaim == null || !Guid.TryParse(sessionIdClaim.Value, out var sessionId))
                    {
                        context.Fail("Token does not contain a valid SessionId.");
                        return;
                    }

                    // Unified cache-aside validation for JWT identical to AdminCookie.
                    // Completely decoupled from the legacy UserTokens DB table lookup.
                    var sessionValidator = context.HttpContext.RequestServices.GetRequiredService<ISessionValidationService>();
                    var isActive = await sessionValidator.IsSessionActiveAsync(sessionId, context.HttpContext.RequestAborted);

                    if (!isActive)
                    {
                        context.Fail("This session has been revoked or expired.");
                    }
                }
            };
        })

        // 3. SECURE COOKIE (For Dashboard Ops)
        .AddCookie("AdminCookie", options =>
        {
            options.Cookie.Name = "BillingFlow.Dashboard.Auth";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.SlidingExpiration = false;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);

            // Revocation & Performance Caching for Dashboard
            options.Events = new CookieAuthenticationEvents
            {
                OnValidatePrincipal = async context =>
                {
                    var sessionIdClaim = context.Principal?.FindFirst(CustomClaimTypes.SessionId);
                    if (sessionIdClaim == null || !Guid.TryParse(sessionIdClaim.Value, out var sessionId))
                    {
                        context.RejectPrincipal();
                        return;
                    }

                    // Unified cache-aside validation
                    var sessionValidator = context.HttpContext.RequestServices.GetRequiredService<ISessionValidationService>();
                    var isActive = await sessionValidator.IsSessionActiveAsync(sessionId, context.HttpContext.RequestAborted);

                    if (!isActive)
                    {
                        context.RejectPrincipal();
                    }
                }
            };
        });

        // 4. Configure Permission-based Authorization Policies
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

        // 5. Register the custom permission handler
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
