// File: src/BillingFlow.Application/Common/Exceptions/InvalidWebhookSignatureException.cs
using System;

namespace BillingFlow.Application.Common.Exceptions;

/// <summary>
/// Thrown when an incoming webhook fails cryptographic validation.
/// This indicates a potential spoofing attempt or misconfigured webhook secret.
/// </summary>
public class InvalidWebhookSignatureException : Exception
{
    public InvalidWebhookSignatureException(string message, Exception innerException)
        : base(message, innerException) { }
}
