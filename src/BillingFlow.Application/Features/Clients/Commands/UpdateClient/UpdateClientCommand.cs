using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Commands.UpdateClient;

public record UpdateClientCommand(
    Guid ClientId,
    string CompanyName,
    string TaxId,
    string Street,
    string City,
    string PostalCode,
    string Country
) : IRequest, IRequirePermission;
