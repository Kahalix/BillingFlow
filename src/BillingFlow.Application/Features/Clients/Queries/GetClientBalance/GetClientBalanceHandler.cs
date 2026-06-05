using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;

using Dapper;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientBalance;

public class GetClientBalanceHandler(
    IDbConnectionFactory connectionFactory,
    TimeProvider timeProvider) : IRequestHandler<GetClientBalanceQuery, ClientBalanceReadModel>
{
    public async Task<ClientBalanceReadModel> Handle(GetClientBalanceQuery request, CancellationToken cancellationToken)
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

        return balance ?? new ClientBalanceReadModel(request.ClientId, 0m, timeProvider.GetUtcNow());
    }
}
