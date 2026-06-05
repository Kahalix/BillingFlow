namespace BillingFlow.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested entity or aggregate is not found in the database.
/// Maps to HTTP 404 Not Found.
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
