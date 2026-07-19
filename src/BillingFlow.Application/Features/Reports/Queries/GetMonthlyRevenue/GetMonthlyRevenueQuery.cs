using System.Collections.Generic;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Reports.Queries.GetMonthlyRevenue;

public record GetMonthlyRevenueQuery(int Year, int Month) : IQuery<IReadOnlyCollection<MonthlyRevenueDto>>, IRequirePermission;
