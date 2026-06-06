using System;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.ProvidedServices.Queries.GetProvidedServiceDetails;

public record ProvidedServiceDetailsDto(
    Guid Id,
    Guid ClientId,
    string CompanyName,
    string Description,
    decimal Amount,
    DateTimeOffset PerformedAt,
    Guid? InvoiceId,
    ProvidedServiceStatus Status
);
