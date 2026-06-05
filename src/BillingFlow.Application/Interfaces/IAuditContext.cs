using System;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Provides extended execution context details for compliance and auditing.
/// Safely handles background jobs, webhooks, and standard HTTP requests.
/// </summary>
public interface IAuditContext
{
    Guid? UserId { get; }
    string? TraceId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? HttpMethod { get; }
    string? RequestPath { get; }
}
