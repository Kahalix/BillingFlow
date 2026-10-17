using System;
using System.Collections.Generic;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Invoices.Common.Models;

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
