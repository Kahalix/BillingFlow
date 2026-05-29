// File: src/BillingFlow.Application/Features/Invoices/Queries/GetInvoiceDetails/GetInvoiceDetailsQuery.cs
using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Features.Invoices.Common.Models;

using MediatR;

namespace BillingFlow.Application.Features.Invoices.Queries.GetInvoiceDetails;

public record GetInvoiceDetailsQuery(Guid InvoiceId) : IRequest<InvoiceDetailsModel>, IRequirePolicy;
