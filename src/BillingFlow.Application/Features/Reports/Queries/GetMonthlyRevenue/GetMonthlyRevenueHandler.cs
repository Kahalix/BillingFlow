using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Dapper;

namespace BillingFlow.Application.Features.Reports.Queries.GetMonthlyRevenue;

public class GetMonthlyRevenueHandler(IDbConnectionFactory connectionFactory)
    : IQueryHandler<GetMonthlyRevenueQuery, IReadOnlyCollection<MonthlyRevenueDto>>
{
    public async Task<Result<IReadOnlyCollection<MonthlyRevenueDto>>> Handle(GetMonthlyRevenueQuery request, CancellationToken cancellationToken)
    {
        // 1. Calculate safe, UTC-based Date Boundaries
        var startDate = new DateTimeOffset(request.Year, request.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = startDate.AddMonths(1);

        // 2. Open a lightweight Read Connection
        using var connection = connectionFactory.CreateConnection();

        // 3. Execute the Stored Procedure safely using Dapper
        var command = new CommandDefinition(
            commandText: "GetMonthlyRevenue",
            parameters: new { StartDate = startDate, EndDate = endDate },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken
        );

        var result = await connection.QueryAsync<MonthlyRevenueDto>(command);

        return Result.Success<IReadOnlyCollection<MonthlyRevenueDto>>(result.ToList().AsReadOnly());
    }
}
