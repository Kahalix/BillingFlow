using System;

using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Xunit;

namespace BillingFlow.Domain.Tests.Entities;

public class UserTokenTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly string _tokenHash = "hashed_value_123";
    private readonly DateTimeOffset _createdAt = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // Token will expire 7 days after creation
    private readonly DateTimeOffset _expiryDate = new DateTimeOffset(2026, 6, 8, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidData_ShouldInitializeCorrectly()
    {
        // Act
        var token = new UserToken(_userId, _sessionId, UserTokenType.RefreshToken, _tokenHash, _expiryDate, _createdAt);

        // Assert
        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(_userId);
        token.SessionId.Should().Be(_sessionId);
        token.Type.Should().Be(UserTokenType.RefreshToken);
        token.TokenHash.Should().Be(_tokenHash);
        token.Expiry.Should().Be(_expiryDate);
        token.CreatedAt.Should().Be(_createdAt);
        token.Data.Should().BeNull();
        token.ConsumedAt.Should().BeNull();
    }

    [Fact]
    public void IsExpired_WhenCurrentTimeIsBeforeExpiry_ShouldReturnFalse()
    {
        // Arrange
        var token = new UserToken(_userId, _sessionId, UserTokenType.RefreshToken, _tokenHash, _expiryDate, _createdAt);

        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_expiryDate.AddDays(-1)); // Simulate time being 1 day before expiration

        // Act
        var isExpired = token.IsExpired(fakeTimeProvider);

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenCurrentTimeIsExactlyAtExpiry_ShouldReturnTrue()
    {
        // Arrange
        var token = new UserToken(_userId, _sessionId, UserTokenType.RefreshToken, _tokenHash, _expiryDate, _createdAt);

        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_expiryDate); // Simulate time exactly on expiration

        // Act
        var isExpired = token.IsExpired(fakeTimeProvider);

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenCurrentTimeIsAfterExpiry_ShouldReturnTrue()
    {
        // Arrange
        var token = new UserToken(_userId, _sessionId, UserTokenType.RefreshToken, _tokenHash, _expiryDate, _createdAt);

        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_expiryDate.AddDays(1)); // Simulate time being 1 day after expiration

        // Act
        var isExpired = token.IsExpired(fakeTimeProvider);

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void MarkAsConsumed_ShouldSetConsumedAtToCurrentTime()
    {
        // Arrange
        var token = new UserToken(_userId, _sessionId, UserTokenType.RefreshToken, _tokenHash, _expiryDate, _createdAt);
        var expectedConsumedTime = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(expectedConsumedTime);

        // Act
        token.MarkAsConsumed(fakeTimeProvider);

        // Assert
        token.ConsumedAt.Should().Be(expectedConsumedTime);
    }

    [Fact]
    public void MarkAsConsumed_WhenAlreadyConsumed_ShouldBeIdempotentAndNotOverwriteTime()
    {
        // Arrange
        var token = new UserToken(_userId, _sessionId, UserTokenType.RefreshToken, _tokenHash, _expiryDate, _createdAt);
        var fakeTimeProvider = new FakeTimeProvider();

        // First consumption
        var firstConsumptionTime = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(firstConsumptionTime);
        token.MarkAsConsumed(fakeTimeProvider);

        // Advance time
        var secondConsumptionTime = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);
        fakeTimeProvider.SetUtcNow(secondConsumptionTime);

        // Act - Attempt to consume again
        token.MarkAsConsumed(fakeTimeProvider);

        // Assert - The original time must be preserved
        token.ConsumedAt.Should().Be(firstConsumptionTime);
    }

    [Fact]
    public void IsActive_WhenNotConsumedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var token = new UserToken(_userId, _sessionId, UserTokenType.RefreshToken, _tokenHash, _expiryDate, _createdAt);

        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_expiryDate.AddDays(-1)); // Time is valid (before expiry)

        // Act
        var isActive = token.IsActive(fakeTimeProvider);

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenConsumedButNotExpired_ShouldReturnFalse()
    {
        // Arrange
        var token = new UserToken(_userId, _sessionId, UserTokenType.RefreshToken, _tokenHash, _expiryDate, _createdAt);
        var fakeTimeProvider = new FakeTimeProvider();

        // 1. Explicitly mutate the state to Consumed
        var consumedTime = _createdAt.AddDays(1);
        fakeTimeProvider.SetUtcNow(consumedTime);
        token.MarkAsConsumed(fakeTimeProvider);

        // 2. Advance time, but keep it safely BEFORE the expiration date.
        // This proves that it is the Consumed STATE, not the Expiry, that invalidates the token.
        var checkTime = _expiryDate.AddDays(-1);
        fakeTimeProvider.SetUtcNow(checkTime);

        // Act
        var isActive = token.IsActive(fakeTimeProvider);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenNotConsumedButExpired_ShouldReturnFalse()
    {
        // Arrange
        var token = new UserToken(_userId, _sessionId, UserTokenType.RefreshToken, _tokenHash, _expiryDate, _createdAt);

        var fakeTimeProvider = new FakeTimeProvider();
        fakeTimeProvider.SetUtcNow(_expiryDate.AddDays(1)); // Simulate time being after expiration

        // Act
        var isActive = token.IsActive(fakeTimeProvider);

        // Assert
        isActive.Should().BeFalse();
    }
}
