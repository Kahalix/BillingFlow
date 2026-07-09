using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// Aggregate Root representing an authenticated user's session lifecycle.
/// Responsible for sliding session validity, hard cap limits, and revocation rules.
/// </summary>
public class UserSession : Entity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Sliding expiration (Idle timeout)
    public DateTimeOffset ExpiresAt { get; private set; }

    // Hard limit (Absolute expiration)
    public DateTimeOffset AbsoluteExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    private UserSession() { }

    public UserSession(
        Guid userId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset absoluteExpiresAt,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User ID cannot be empty.");

        if (expiresAt <= createdAt)
            throw new DomainException("Session expiration must be after creation.");

        if (absoluteExpiresAt < expiresAt)
            throw new DomainException("Absolute expiration cannot be earlier than the initial sliding expiration.");

        Id = Guid.NewGuid();
        UserId = userId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        AbsoluteExpiresAt = absoluteExpiresAt;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        LastSeenAt = createdAt;
    }

    /// <summary>
    /// Extends the session's sliding expiration window without exceeding the absolute hard cap.
    /// Updates telemetry (LastSeenAt).
    /// </summary>
    public void RefreshActivity(DateTimeOffset now, TimeSpan inactivityLifetime)
    {
        if (!IsActive(now)) return;

        var newExpiry = now.Add(inactivityLifetime);

        // Ensure the sliding window never breaches the absolute maximum lifetime
        ExpiresAt = newExpiry < AbsoluteExpiresAt ? newExpiry : AbsoluteExpiresAt;
        LastSeenAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt.HasValue) return;
        RevokedAt = now;
    }

    //public void TrackActivity(DateTimeOffset now)
    //{
    //    if (!IsActive(now)) return;
    //    LastSeenAt = now;
    //}

    public bool IsExpired(DateTimeOffset now) =>
        ExpiresAt <= now || AbsoluteExpiresAt <= now;

    public bool IsActive(DateTimeOffset now) =>
        RevokedAt == null && !IsExpired(now);
}
