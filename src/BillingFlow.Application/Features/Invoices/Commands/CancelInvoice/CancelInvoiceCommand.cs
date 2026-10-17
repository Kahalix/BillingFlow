using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Invoices.Commands.CancelInvoice;

/// <summary>
/// Voids an invoice.
/// </summary>
public record CancelInvoiceCommand(Guid InvoiceId) : ICommand, IRequirePermission;
