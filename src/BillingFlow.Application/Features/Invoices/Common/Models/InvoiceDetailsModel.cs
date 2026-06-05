using System;
using System.Collections.Generic;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Invoices.Common.Models;

public record InvoiceItemModel(
    string Description,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal
);

public record InvoiceClientModel(
    Guid ClientId,
    string CompanyName,
    string TaxId
);

public record InvoiceDetailsModel(
    Guid Id,
    string InvoiceNumber,
    InvoiceClientModel Client,
    decimal TotalAmount,
    decimal PaidAmount,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    InvoiceStatus Status,
    IReadOnlyCollection<InvoiceItemModel> Items
);
