// File: src/BillingFlow.Domain/Exceptions/ForbiddenException.cs
namespace BillingFlow.Domain.Exceptions;

/// <summary>
/// Exception thrown when a domain action is prohibited due to policy or security constraints.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message)
    {
    }
}
