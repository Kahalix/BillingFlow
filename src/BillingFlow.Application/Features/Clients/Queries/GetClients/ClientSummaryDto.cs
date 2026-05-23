// File: src/BillingFlow.Application/Features/Clients/Queries/GetClients/ClientSummaryDto.cs
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Clients.Queries.GetClients;

public record ClientSummaryDto(
    Guid Id,
    string CompanyName,
    string TaxId,
    ClientStatus Status
);
