using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.DashboardLogout;

/// <summary>
/// Command to durably revoke a dashboard session from the database.
/// </summary>
public record DashboardLogoutCommand : ICommand, IAuthenticatedRequest;
