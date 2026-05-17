// File: src/BillingFlow.Infrastructure/BackgroundJobs/HangfireAuthorizationFilter.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;

using Hangfire.Dashboard;

using Microsoft.Extensions.DependencyInjection;

namespace BillingFlow.Infrastructure.BackgroundJobs;

/// <summary>
/// Restricts access to the Hangfire Dashboard in production environments.
/// Ensures only users with the Admin role can view sensitive background job data.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            return false;
        }

        // Resolve ICurrentUserService from the DI container for the current HTTP request
        var currentUserService = httpContext.RequestServices.GetRequiredService<ICurrentUserService>();

        // Ensure the user is authenticated and strongly-typed role matches Admin
        return currentUserService.IsAuthenticated && currentUserService.UserRole == Role.Admin;
    }
}
