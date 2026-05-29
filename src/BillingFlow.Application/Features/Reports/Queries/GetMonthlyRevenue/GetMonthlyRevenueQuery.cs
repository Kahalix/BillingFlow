// File: src/BillingFlow.Application/Features/Reports/Queries/GetMonthlyRevenue/GetMonthlyRevenueQuery.cs
using System.Collections.Generic;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Reports.Queries.GetMonthlyRevenue;

public record GetMonthlyRevenueQuery(int Year, int Month) : IRequest<IReadOnlyCollection<MonthlyRevenueDto>>, IRequirePermission;
