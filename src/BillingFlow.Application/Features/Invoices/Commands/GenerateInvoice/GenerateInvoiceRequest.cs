// File: src/BillingFlow.Application/Features/Invoices/Commands/GenerateInvoice/GenerateInvoiceRequest.cs
using System;

namespace BillingFlow.Application.Features.Invoices.Commands.GenerateInvoice;

/// <summary>
/// HTTP payload schema for triggering automated invoice compilation.
/// </summary>
public record GenerateInvoiceRequest(
    Guid ClientId,
    DateTimeOffset FromDate,
    DateTimeOffset ToDate
);
