// File: src/BillingFlow.Application/Features/Clients/Queries/GetClientDetails/ClientDetailsResponse.cs
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientDetails;

public record ClientDetailsResponse(
    Guid Id,
    Guid? UserId,
    string CompanyName,
    string TaxId,
    string Street,
    string City,
    string PostalCode,
    string Country,
    ClientStatus Status
);
