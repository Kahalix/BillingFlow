using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Models;
using BillingFlow.Domain.Enums;

using MediatR;

namespace BillingFlow.Application.Features.Invoices.Queries.GetInvoices;

public record GetInvoicesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? FilterByClientId = null,
    InvoiceStatus? FilterByStatus = null,
    string? SearchTerm = null) : IRequest<PaginatedList<InvoiceSummaryDto>>, IRequirePermission;
