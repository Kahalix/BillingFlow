using System;

using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Exceptions;

using FluentAssertions;

using Xunit;

namespace BillingFlow.Domain.Tests.Entities;

public class UserSessionTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _ipAddress = "192.168.1.100";
    private readonly string _userAgent = "Mozilla/5.0";

    private readonly DateTimeOffset _createdAt = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);

    // Initial sliding expiration (e.g., 7 days)
    private readonly DateTimeOffset _initialExpiresAt = new DateTimeOffset(2026, 6, 8, 10, 0, 0, TimeSpan.Zero);

    // Absolute hard cap (e.g., 30 days)
    private readonly DateTimeOffset _absoluteExpiresAt = new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);

    #region Constructor & Invariants

    [Fact]
    public void Create_ValidData_ShouldInitializeCorrectly()
    {
        // Act
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt, _ipAddress, _userAgent);

        // Assert
        session.Id.Should().NotBeEmpty();
        session.UserId.Should().Be(_userId);
        session.CreatedAt.Should().Be(_createdAt);
        session.ExpiresAt.Should().Be(_initialExpiresAt);
        session.AbsoluteExpiresAt.Should().Be(_absoluteExpiresAt);
        session.LastSeenAt.Should().Be(_createdAt);
        session.IpAddress.Should().Be(_ipAddress);
        session.UserAgent.Should().Be(_userAgent);
        session.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowDomainException()
    {
        // Act
        Action action = () => new UserSession(Guid.Empty, _createdAt, _initialExpiresAt, _absoluteExpiresAt);

        // Assert
        action.Should().Throw<DomainException>().WithMessage("User ID cannot be empty.");
    }

    [Theory]
    [InlineData(-1)] // Before creation
    [InlineData(0)]  // Exactly at creation time
    public void Create_WithInvalidExpiresAt_ShouldThrowDomainException(int daysOffset)
    {
        // Arrange
        var invalidExpiresAt = _createdAt.AddDays(daysOffset);

        // Act
        Action action = () => new UserSession(_userId, _createdAt, invalidExpiresAt, _absoluteExpiresAt);

        // Assert
        action.Should().Throw<DomainException>().WithMessage("Session expiration must be after creation.");
    }

    [Fact]
    public void Create_WithAbsoluteExpiryBeforeSlidingExpiry_ShouldThrowDomainException()
    {
        // Arrange
        var invalidAbsolute = _initialExpiresAt.AddSeconds(-1);

        // Act
        Action action = () => new UserSession(_userId, _createdAt, _initialExpiresAt, invalidAbsolute);

        // Assert
        action.Should().Throw<DomainException>().WithMessage("Absolute expiration cannot be earlier than the initial sliding expiration.");
    }

    [Fact]
    public void Create_WithAbsoluteExpiryEqualToSlidingExpiry_ShouldSucceed()
    {
        // Act
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _initialExpiresAt);

        // Assert
        session.AbsoluteExpiresAt.Should().Be(session.ExpiresAt);
    }

    #endregion

    #region Sliding Expiration (RefreshActivity)

    [Fact]
    public void RefreshActivity_WhenActiveAndWithinHardCap_ShouldExtendExpiry()
    {
        // Arrange
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);

        var now = _createdAt.AddDays(1); // Day 2
        var slidingWindow = TimeSpan.FromDays(7);
        var expectedNewExpiry = now.Add(slidingWindow); // Day 9 (still well below absolute Day 30)

        // Act
        session.RefreshActivity(now, slidingWindow);

        // Assert
        session.ExpiresAt.Should().Be(expectedNewExpiry);
        session.LastSeenAt.Should().Be(now);
    }

    [Fact]
    public void RefreshActivity_WhenActiveAndExceedsHardCap_ShouldCapAtAbsoluteExpiry()
    {
        // Arrange
        var now = _createdAt.AddDays(25); // Day 26

        // Simulate a session that has been kept alive by the user 
        // and is still valid on Day 26 (e.g., expires on Day 28)
        var currentValidExpiresAt = now.AddDays(2);

        var session = new UserSession(_userId, _createdAt, currentValidExpiresAt, _absoluteExpiresAt);

        var slidingWindow = TimeSpan.FromDays(7);

        // Act
        // Attempt to extend the session by 7 days.
        // now (Day 26) + 7 days = Day 33.
        // This exceeds the absolute limit (_absoluteExpiresAt = Day 30).
        session.RefreshActivity(now, slidingWindow);

        // Assert
        session.ExpiresAt.Should().Be(_absoluteExpiresAt);
        session.LastSeenAt.Should().Be(now);
    }

    [Fact]
    public void RefreshActivity_WhenSessionIsExpired_ShouldNotUpdateExpiryOrLastSeenAt()
    {
        // Arrange
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);

        var now = _initialExpiresAt.AddDays(1); // Session expired yesterday
        var slidingWindow = TimeSpan.FromDays(7);

        // Act
        session.RefreshActivity(now, slidingWindow);

        // Assert
        session.ExpiresAt.Should().Be(_initialExpiresAt); // Remains unchanged
        session.LastSeenAt.Should().Be(_createdAt); // Remains unchanged
    }

    [Fact]
    public void RefreshActivity_WhenSessionIsRevoked_ShouldNotUpdateExpiryOrLastSeenAt()
    {
        // Arrange
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);

        var revokeTime = _createdAt.AddDays(1);
        session.Revoke(revokeTime);

        var now = _createdAt.AddDays(2); // Try to refresh after revocation
        var slidingWindow = TimeSpan.FromDays(7);

        // Act
        session.RefreshActivity(now, slidingWindow);

        // Assert
        session.ExpiresAt.Should().Be(_initialExpiresAt); // Remains unchanged
        session.LastSeenAt.Should().Be(_createdAt); // Remains unchanged
    }

    #endregion

    #region Revocation

    [Fact]
    public void Revoke_ShouldSetRevokedAtToCurrentTime()
    {
        // Arrange
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);
        var now = _createdAt.AddDays(1);

        // Act
        session.Revoke(now);

        // Assert
        session.RevokedAt.Should().Be(now);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldBeIdempotentAndNotOverwriteTime()
    {
        // Arrange
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);
        var firstRevokeTime = _createdAt.AddDays(1);
        var secondRevokeTime = _createdAt.AddDays(2);

        // First revocation
        session.Revoke(firstRevokeTime);

        // Act - Attempt to revoke again later
        session.Revoke(secondRevokeTime);

        // Assert - The original time must be preserved
        session.RevokedAt.Should().Be(firstRevokeTime);
    }

    #endregion

    #region State Checks (IsActive / IsExpired)

    [Fact]
    public void IsExpired_WhenBeforeExpiry_ShouldBeFalse()
    {
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);
        session.IsExpired(_initialExpiresAt.AddSeconds(-1)).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExactlyAtExpiry_ShouldBeTrue()
    {
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);
        session.IsExpired(_initialExpiresAt).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenAfterExpiry_ShouldBeTrue()
    {
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);
        session.IsExpired(_initialExpiresAt.AddSeconds(1)).Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenNotRevokedAndNotExpired_ShouldBeTrue()
    {
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);
        session.IsActive(_initialExpiresAt.AddDays(-1)).Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenExactlyAtExpiry_ShouldBeFalse()
    {
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);
        session.IsActive(_initialExpiresAt).Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenRevokedButNotExpired_ShouldBeFalse()
    {
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);

        var revokeTime = _createdAt.AddDays(1);
        session.Revoke(revokeTime);

        // Time is valid in terms of expiration, but session was revoked
        var checkTime = _initialExpiresAt.AddDays(-1);
        session.IsActive(checkTime).Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenNotRevokedButExpired_ShouldBeFalse()
    {
        var session = new UserSession(_userId, _createdAt, _initialExpiresAt, _absoluteExpiresAt);
        session.IsActive(_initialExpiresAt.AddDays(1)).Should().BeFalse();
    }

    #endregion
}
