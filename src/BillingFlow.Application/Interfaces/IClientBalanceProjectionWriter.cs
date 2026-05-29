// File: src/BillingFlow.Application/Interfaces/IClientBalanceProjectionWriter.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Dedicated contract for updating the materialized Client Balance read model.
/// </summary>
public interface IClientBalanceProjectionWriter
{
    /// <summary>
    /// Applies a change to the client's current debt. 
    /// Positive values increase the debt (new invoices), negative values decrease it (payments).
    /// </summary>
    Task ApplyDebtDeltaAsync(Guid clientId, decimal deltaAmount, DateTimeOffset updatedAt, CancellationToken cancellationToken = default);
}
