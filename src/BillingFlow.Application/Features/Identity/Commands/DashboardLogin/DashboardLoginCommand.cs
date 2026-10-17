using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Identity.Commands.DashboardLogin;

/// <summary>
/// Command to authenticate a system administrator and spawn an infrastructure dashboard session.
/// </summary>
public record DashboardLoginCommand(string Email, string Password) : ICommand<DashboardLoginResponse>, IPublicRequest;
