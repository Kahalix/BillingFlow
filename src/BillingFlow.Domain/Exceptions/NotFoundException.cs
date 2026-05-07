// File: src/BillingFlow.Domain/Exceptions/NotFoundException.cs
namespace BillingFlow.Domain.Exceptions;

/// <summary>
/// Exception thrown when a requested entity or aggregate is not found in the domain.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' ({key}) was not found.")
    {
    }
}
