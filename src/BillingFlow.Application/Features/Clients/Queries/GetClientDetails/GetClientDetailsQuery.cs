using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientDetails;

/// <summary>
/// Retrieves the full profile details of a specific billing client by ID.
/// Intended for back-office employees.
/// </summary>
public record GetClientDetailsQuery(Guid ClientId) : IQuery<ClientDetailsResponse>, IRequirePermission;
