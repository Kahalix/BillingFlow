using System;

namespace BillingFlow.Application.Features.ProvidedServices.Commands.UpdateProvidedService;

public record UpdateProvidedServiceRequest(
    string Description,
    decimal Amount,
    DateTimeOffset PerformedAt
);
