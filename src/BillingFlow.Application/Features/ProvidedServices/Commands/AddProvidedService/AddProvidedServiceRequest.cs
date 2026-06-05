namespace BillingFlow.Application.Features.ProvidedServices.Commands.AddProvidedService;

public record AddProvidedServiceRequest(
    string Description,
    decimal Amount,
    DateTimeOffset PerformedAt
);
