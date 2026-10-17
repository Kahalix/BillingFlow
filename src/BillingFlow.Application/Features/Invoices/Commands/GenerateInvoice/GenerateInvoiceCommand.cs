using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Invoices.Commands.GenerateInvoice;

public record GenerateInvoiceCommand(
    Guid ClientId,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate
) : ICommand<Guid>, IRequirePermission;
