// File: src/BillingFlow.Application/Common/Exceptions/UnauthorizedException.cs
namespace BillingFlow.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a user is not authenticated or their session is invalid.
/// Maps to HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
