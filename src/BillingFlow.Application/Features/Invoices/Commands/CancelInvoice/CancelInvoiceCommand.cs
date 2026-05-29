// File: src/BillingFlow.Application/Features/Invoices/Commands/CancelInvoice/CancelInvoiceCommand.cs
using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Invoices.Commands.CancelInvoice;

/// <summary>
/// Voids an invoice.
/// </summary>
public record CancelInvoiceCommand(Guid InvoiceId) : IRequest, IRequirePermission;
