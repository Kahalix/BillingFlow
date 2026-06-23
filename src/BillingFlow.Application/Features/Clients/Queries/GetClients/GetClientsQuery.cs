using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Models;
using BillingFlow.Domain.Enums;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Queries.GetClients;

/// <summary>
/// Retrieves a paginated list of clients.
/// Supports optional searching by name/TaxID and filtering by Status.
/// </summary>
public record GetClientsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    ClientStatus? Status = null
) : IRequest<PaginatedList<ClientSummaryDto>>, IRequirePolicy;
