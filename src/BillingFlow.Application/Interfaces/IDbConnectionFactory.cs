// File: src/BillingFlow.Application/Interfaces/IDbConnectionFactory.cs
using System.Data;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Abstraction for creating raw database connections.
/// Dedicated specifically for the Read Side (Queries) using micro-ORMs like Dapper,
/// completely decoupling reads from the Entity Framework Core DbContext.
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
