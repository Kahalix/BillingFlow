// File: src/BillingFlow.Application/Features/Invoices/Queries/GetInvoices/InvoiceSummaryDto.cs
using System;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Invoices.Queries.GetInvoices;

public record InvoiceSummaryDto(
    Guid Id,
    string InvoiceNumber,
    Guid ClientId,
    string CompanyName,
    decimal TotalAmount,
    decimal PaidAmount,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    InvoiceStatus Status
);
