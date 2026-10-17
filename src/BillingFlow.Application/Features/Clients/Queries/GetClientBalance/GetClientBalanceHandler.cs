using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Dapper;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientBalance;

public class GetClientBalanceHandler(
    IDbConnectionFactory connectionFactory,
    TimeProvider timeProvider) : IQueryHandler<GetClientBalanceQuery, ClientBalanceReadModel>
{
    public async Task<Result<ClientBalanceReadModel>> Handle(GetClientBalanceQuery request, CancellationToken cancellationToken)
    {
        // 1. We create a fresh connection specifically for this read operation.
        // 2. We safely use 'using' because we own this instance's lifecycle, keeping it out of EF Core's hands.
        using var connection = connectionFactory.CreateConnection();

        const string sql = """
            SELECT ClientId, CurrentDebt, UpdatedAt 
            FROM ClientBalances 
            WHERE ClientId = @ClientId
            """;

        // Dapper automatically handles connection.OpenAsync() if it is closed.
        var balance = await connection.QuerySingleOrDefaultAsync<ClientBalanceReadModel>(
            sql,
            new { ClientId = request.ClientId });

        return Result.Success(balance ?? new ClientBalanceReadModel(request.ClientId, 0m, timeProvider.GetUtcNow()));
    }
}
