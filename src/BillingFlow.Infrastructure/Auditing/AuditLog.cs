using System;

namespace BillingFlow.Infrastructure.Auditing;

/// <summary>
/// Infrastructure-only record of a data change within the system.
/// Not part of the business domain; strictly for compliance, debugging, and tracing.
/// </summary>
public class AuditLog
{
    public Guid Id { get; private set; }
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty; // "Insert", "Update", "Delete"
    public Guid? UserId { get; private set; }

    // Extended Environment Context
    public string? TraceId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? HttpMethod { get; private set; }
    public string? RequestPath { get; private set; }

    public string? OldValues { get; private set; } // JSON
    public string? NewValues { get; private set; } // JSON
    public DateTimeOffset Timestamp { get; private set; }

    // EF Core constructor
    protected AuditLog() { }

    public AuditLog(
        string entityName,
        string entityId,
        string action,
        Guid? userId,
        string? traceId,
        string? ipAddress,
        string? userAgent,
        string? httpMethod,
        string? requestPath,
        string? oldValues,
        string? newValues,
        DateTimeOffset timestamp)
    {
        Id = Guid.NewGuid();
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        UserId = userId;
        TraceId = traceId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        HttpMethod = httpMethod;
        RequestPath = requestPath;
        OldValues = oldValues;
        NewValues = newValues;
        Timestamp = timestamp;
    }
}
