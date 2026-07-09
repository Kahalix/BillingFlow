using BillingFlow.Domain.Enums;

using Hangfire.Dashboard;

namespace BillingFlow.Infrastructure.BackgroundJobs;

/// <summary>
/// Execution relies on the ASP.NET Core Policy Scheme (SmartRouter) resolving the identity upstream.
/// Network boundaries (IP allowlists) are handled at the Edge Gateway (NGINX).
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext is null || !(httpContext.User.Identity?.IsAuthenticated ?? false))
        {
            return false;
        }

        return httpContext.User.IsInRole(Role.Admin.ToString());
    }
}
