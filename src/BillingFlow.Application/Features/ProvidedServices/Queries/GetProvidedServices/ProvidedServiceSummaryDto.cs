using System;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServices;

public record ProvidedServiceSummaryDto(
    Guid Id,
    Guid ClientId,
    string CompanyName,
    string Description,
    decimal Amount,
    DateTimeOffset PerformedAt,
    Guid? InvoiceId,
    ProvidedServiceStatus Status
);
