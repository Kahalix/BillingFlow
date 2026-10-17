using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Clients.Commands.CreateClient;

public record CreateClientCommand(
    Guid? UserId, // Optional: A client profile can be created before a user account is linked
    string CompanyName,
    string TaxId,
    string Street,
    string City,
    string PostalCode,
    string Country
) : ICommand<CreateClientResponse>, IRequirePermission;
