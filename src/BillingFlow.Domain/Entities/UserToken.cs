using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// Represents a security token used for authentication and one-time security operations.
/// Static factory methods enforce the invariants required by each token type.
/// </summary>
public class UserToken : Entity
{
    public Guid UserId { get; private set; }
    public Guid? SessionId { get; private set; }
    public UserTokenType Type { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string? Data { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset Expiry { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    // Concurrency token to guarantee prevention of token replay attacks
    public byte[]? RowVersion { get; private set; }

    protected UserToken() { }

    private UserToken(
        Guid userId,
        Guid? sessionId,
        UserTokenType type,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiry,
        string? data = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new DomainException("Token hash cannot be empty.");

        if (expiry <= createdAt)
            throw new DomainException("Token expiration time must be strictly after creation time.");

        Id = Guid.NewGuid();
        UserId = userId;
        SessionId = sessionId;
        Type = type;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        Expiry = expiry;
        Data = data;
    }

    // --- STATIC FACTORY METHODS ---

    public static UserToken CreateRefreshToken(Guid userId, Guid sessionId, string hash, DateTimeOffset createdAt, DateTimeOffset expiry)
    {
        if (sessionId == Guid.Empty)
            throw new DomainException("Refresh tokens must be strictly bound to a valid session ID.");

        return new UserToken(userId, sessionId, UserTokenType.RefreshToken, hash, createdAt, expiry);
    }

    public static UserToken CreatePasswordResetToken(Guid userId, string hash, DateTimeOffset createdAt, DateTimeOffset expiry)
    {
        return new UserToken(userId, null, UserTokenType.PasswordReset, hash, createdAt, expiry);
    }

    public static UserToken CreateEmailConfirmationToken(Guid userId, string hash, string newEmail, DateTimeOffset createdAt, DateTimeOffset expiry)
    {
        if (string.IsNullOrWhiteSpace(newEmail))
            throw new DomainException("Email confirmation tokens require the new email address as payload data.");

        return new UserToken(userId, null, UserTokenType.EmailConfirmation, hash, createdAt, expiry, data: newEmail.Trim().ToLowerInvariant());
    }

    // --- STATE MUTATIONS ---

    public void MarkAsConsumed(DateTimeOffset now)
    {
        // Idempotency guard
        if (ConsumedAt.HasValue) return;

        ConsumedAt = now;
    }

    public bool IsExpired(DateTimeOffset now) => now >= Expiry;

    public bool IsActive(DateTimeOffset now) => !ConsumedAt.HasValue && !IsExpired(now);
}
