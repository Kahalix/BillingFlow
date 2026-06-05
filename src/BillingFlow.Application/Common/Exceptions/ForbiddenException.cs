namespace BillingFlow.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when an authenticated user lacks the required permissions or roles.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message)
    {
    }
}
