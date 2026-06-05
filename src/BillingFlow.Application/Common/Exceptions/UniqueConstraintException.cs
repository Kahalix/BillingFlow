using System;

namespace BillingFlow.Application.Common.Exceptions;

/// <summary>
/// Thrown when a unique constraint is violated in the persistence layer.
/// Keeps the Application layer agnostic of specific databases while identifying the failing domain entity.
/// </summary>
public class UniqueConstraintException : Exception
{
    /// <summary>
    /// The name of the domain entity that triggered the unique constraint violation (e.g., "StripeEventLog").
    /// </summary>
    public string? EntityName { get; }

    public UniqueConstraintException(string message, string? entityName, Exception innerException)
        : base(message, innerException)
    {
        EntityName = entityName;
    }
}
