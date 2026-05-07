// File: src/BillingFlow.Domain/Exceptions/DomainException.cs
namespace BillingFlow.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-specific validation or business rule violations.
/// E.g., trying to pay an invoice that is already paid, or creating a service for a suspended client.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
