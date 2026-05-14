// File: src/BillingFlow.Domain/Entities/UserToken.cs
using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;

namespace BillingFlow.Domain.Entities;

public class UserToken : Entity
{
    public Guid UserId { get; private set; }
    public Guid SessionId { get; private set; }
    public UserTokenType Type { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset Expiry { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public bool IsExpired(TimeProvider timeProvider) => timeProvider.GetUtcNow() >= Expiry;
    public bool IsActive(TimeProvider timeProvider) => !ConsumedAt.HasValue && !IsExpired(timeProvider);

    protected UserToken() { }

    public UserToken(Guid userId, Guid sessionId, UserTokenType type, string tokenHash, DateTimeOffset expiry)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SessionId = sessionId;
        Type = type;
        TokenHash = tokenHash;
        Expiry = expiry;
    }

    public void MarkAsConsumed(TimeProvider timeProvider)
    {
        ConsumedAt = timeProvider.GetUtcNow();
    }
}
