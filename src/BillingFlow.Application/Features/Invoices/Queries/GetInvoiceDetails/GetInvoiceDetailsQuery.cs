using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Features.Invoices.Common.Models;

namespace BillingFlow.Application.Features.Invoices.Queries.GetInvoiceDetails;

public record GetInvoiceDetailsQuery(Guid InvoiceId) : IQuery<InvoiceDetailsModel>, IRequirePolicy;
