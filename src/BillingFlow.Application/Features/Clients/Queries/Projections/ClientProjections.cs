using System.Linq.Expressions;

using BillingFlow.Application.Features.Clients.Queries.GetClientDetails;
using BillingFlow.Domain.Entities;

namespace BillingFlow.Application.Features.Clients.Queries.Projections;

/// <summary>
/// Shared EF Core projections to maintain DRY principles across multiple queries.
/// </summary>
public static class ClientProjections
{
    // This Expression is translated directly into a SQL SELECT clause by Entity Framework Core.
    public static Expression<Func<Client, ClientDetailsResponse>> Details =>
        c => new ClientDetailsResponse(
            c.Id,
            c.UserId,
            c.CompanyName,
            c.TaxId,
            c.Address.Street,
            c.Address.City,
            c.Address.PostalCode,
            c.Address.Country,
            c.Status
        );
}
