using System;
using System.Collections.Generic;

using BillingFlow.Application.Features.Invoices.Common.Models;
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Tests.Helpers;

/// <summary>
/// A centralized Test Data Builder specifically for Application Layer DTOs and Read Models.
/// Keeps Domain testing boundaries clean.
/// </summary>
public static class ApplicationDtoFactory
{
    /// <summary>
    /// Creates a mock Data Transfer Object (DTO) for Application layer queries.
    /// </summary>
    public static InvoiceDetailsModel CreateInvoiceDetailsModel(
        Guid? invoiceId = null,
        string invoiceNumber = "INV/001")
    {
        return new InvoiceDetailsModel(
            invoiceId ?? Guid.NewGuid(),
            invoiceNumber,
            new InvoiceClientModel(Guid.NewGuid(), "Test Corp", "TAX-123"),
            TotalAmount: 1000m,
            PaidAmount: 0m,
            IssueDate: DateTimeOffset.UtcNow,
            DueDate: DateTimeOffset.UtcNow.AddDays(14),
            Status: InvoiceStatus.Unpaid,
            Items: new List<InvoiceItemModel>()
        );
    }
}
