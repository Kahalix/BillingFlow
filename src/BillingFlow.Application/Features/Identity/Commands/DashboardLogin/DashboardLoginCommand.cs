using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.DashboardLogin;

/// <summary>
/// Command to authenticate a system administrator and spawn an infrastructure dashboard session.
/// </summary>
public record DashboardLoginCommand(string Email, string Password) : IRequest<DashboardLoginResponse>, IPublicRequest;
