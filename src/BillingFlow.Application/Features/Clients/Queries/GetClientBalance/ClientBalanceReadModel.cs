using System;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientBalance;

/// <summary>
/// Strict CQRS Read Model. A flat DTO representing the materialized projection of a client's balance.
/// </summary>
public record ClientBalanceReadModel(Guid ClientId, decimal CurrentDebt, DateTimeOffset UpdatedAt);
