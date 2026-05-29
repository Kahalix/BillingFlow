// File: src/BillingFlow.Application/Features/Invoices/Commands/GenerateInvoice/GenerateInvoiceCommand.cs
using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Invoices.Commands.GenerateInvoice;

public record GenerateInvoiceCommand(
    Guid ClientId,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate
) : IRequest<Guid>, IRequirePermission;
