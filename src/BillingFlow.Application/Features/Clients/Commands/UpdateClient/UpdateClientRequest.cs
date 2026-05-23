// File: src/BillingFlow.Application/Features/Clients/Commands/UpdateClient/UpdateClientRequest.cs
namespace BillingFlow.Application.Features.Clients.Commands.UpdateClient;
public record UpdateClientRequest(
    string CompanyName,
    string TaxId,
    string Street,
    string City,
    string PostalCode,
    string Country
);
