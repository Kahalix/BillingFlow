// File: src/BillingFlow.Application/Features/Clients/Queries/GetMyClientDetails/GetMyClientDetailsQuery.cs
using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Features.Clients.Queries.GetClientDetails;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Queries.GetMyClientDetails;

/// <summary>
/// Retrieves the profile details of the currently authenticated user's client profile.
/// Inherent data ownership: no explicit policy needed, scoped via CurrentUser context.
/// </summary>
public record GetMyClientDetailsQuery : IRequest<ClientDetailsResponse>, IAuthenticatedRequest;
