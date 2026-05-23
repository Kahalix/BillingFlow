// File: src/BillingFlow.Application/Features/Clients/Queries/GetClientDetails/GetClientDetailsQuery.cs
using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientDetails;

/// <summary>
/// Retrieves the full profile details of a specific billing client by ID.
/// Intended for back-office employees.
/// </summary>
public record GetClientDetailsQuery(Guid ClientId) : IRequest<ClientDetailsResponse>, IRequirePermission;
