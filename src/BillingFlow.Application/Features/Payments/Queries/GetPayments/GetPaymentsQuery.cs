// File: src/BillingFlow.Application/Features/Payments/Queries/GetPayments/GetPaymentsQuery.cs
using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Models;
using BillingFlow.Domain.Enums;

using MediatR;

namespace BillingFlow.Application.Features.Payments.Queries.GetPayments;

public record GetPaymentsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    Guid? FilterByClientId = null,
    Guid? FilterByInvoiceId = null,
    PaymentProvider? FilterByProvider = null,
    PaymentMethod? FilterByMethod = null,
    string? SearchTerm = null) : IRequest<PaginatedList<PaymentSummaryDto>>, IRequirePermission;
