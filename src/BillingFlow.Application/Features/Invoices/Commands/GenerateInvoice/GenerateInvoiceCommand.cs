using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Invoices.Commands.GenerateInvoice;

public record GenerateInvoiceCommand(
    Guid ClientId,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate
) : IRequest<Guid>, IRequirePermission;
