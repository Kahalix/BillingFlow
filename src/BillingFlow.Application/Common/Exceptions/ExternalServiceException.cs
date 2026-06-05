using System;

namespace BillingFlow.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when an external integration (like Stripe or an SMTP server) fails.
/// Maps to HTTP 502 Bad Gateway or 503 Service Unavailable.
/// </summary>
public class ExternalServiceException : Exception
{
    /// <summary>
    /// Indicates if the error is temporary (e.g., network timeout, 5xx server error).
    /// If true, the system might try to self-heal or retry the operation later.
    /// </summary>
    public bool IsTransient { get; }

    public ExternalServiceException(string message, bool isTransient, Exception innerException)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }
}
