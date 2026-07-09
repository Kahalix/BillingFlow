using System;

using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using FluentAssertions;

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

    #region Factory Methods (Happy Paths)

    [Fact]
    public void CreateRefreshToken_ValidData_ShouldInitializeCorrectly()
    {
        // Act
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);

        // Assert
        token.Id.Should().NotBeEmpty();
        token.UserId.Should().Be(_userId);

        // Explicitly verify the session binding
        token.SessionId.Should().Be(_sessionId);

        token.Type.Should().Be(UserTokenType.RefreshToken);
        token.TokenHash.Should().Be(_tokenHash);
        token.Expiry.Should().Be(_expiryDate);
        token.CreatedAt.Should().Be(_createdAt);
        token.Data.Should().BeNull();
        token.ConsumedAt.Should().BeNull();
    }

    [Fact]
    public void CreatePasswordResetToken_ValidData_ShouldInitializeCorrectly()
    {
        // Act
        var token = UserToken.CreatePasswordResetToken(_userId, _tokenHash, _createdAt, _expiryDate);

        // Assert
        token.Type.Should().Be(UserTokenType.PasswordReset);
        token.SessionId.Should().BeNull();
    }

    [Fact]
    public void CreateEmailConfirmationToken_ValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var newEmail = "  NEW@example.com  ";

        // Act
        var token = UserToken.CreateEmailConfirmationToken(_userId, _tokenHash, newEmail, _createdAt, _expiryDate);

        // Assert
        token.Type.Should().Be(UserTokenType.EmailConfirmation);

        // Domain normalizes email by trimming and lower-casing
        token.Data.Should().Be("new@example.com");
        token.SessionId.Should().BeNull();
    }

    [Theory]
    [InlineData("TEST@EXAMPLE.COM", "test@example.com")]
    [InlineData(" Test@Example.com ", "test@example.com")]
    [InlineData("USER@DOMAIN.PL", "user@domain.pl")]
    public void CreateEmailConfirmationToken_ShouldNormalizeEmail(string input, string expected)
    {
        // Act
        var token = UserToken.CreateEmailConfirmationToken(_userId, _tokenHash, input, _createdAt, _expiryDate);

        // Assert
        token.Data.Should().Be(expected);
    }

    #endregion

    #region Edge Cases (Domain Exceptions)

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrowDomainException()
    {
        // Act
        Action action = () => UserToken.CreatePasswordResetToken(Guid.Empty, _tokenHash, _createdAt, _expiryDate);

        // Assert
        action.Should().Throw<DomainException>().WithMessage("User ID cannot be empty.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyTokenHash_ShouldThrowDomainException(string? emptyHash)
    {
        // Act
        Action action = () => UserToken.CreatePasswordResetToken(_userId, emptyHash ?? string.Empty, _createdAt, _expiryDate);

        // Assert
        action.Should().Throw<DomainException>().WithMessage("Token hash cannot be empty.");
    }

    [Theory]
    [InlineData(-1)] // Before creation
    [InlineData(0)]  // Exactly at creation time
    public void Create_WithInvalidExpiry_ShouldThrowDomainException(int daysOffset)
    {
        // Arrange
        var invalidExpiry = _createdAt.AddDays(daysOffset);

        // Act
        Action action = () => UserToken.CreatePasswordResetToken(_userId, _tokenHash, _createdAt, invalidExpiry);

        // Assert
        action.Should().Throw<DomainException>().WithMessage("Token expiration time must be strictly after creation time.");
    }

    [Fact]
    public void CreateRefreshToken_WithEmptySessionId_ShouldThrowDomainException()
    {
        // Act
        Action action = () => UserToken.CreateRefreshToken(_userId, Guid.Empty, _tokenHash, _createdAt, _expiryDate);

        // Assert
        action.Should().Throw<DomainException>().WithMessage("Refresh tokens must be strictly bound to a valid session ID.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateEmailConfirmationToken_WithEmptyEmail_ShouldThrowDomainException(string? emptyEmail)
    {
        // Act
        Action action = () => UserToken.CreateEmailConfirmationToken(_userId, _tokenHash, emptyEmail ?? string.Empty, _createdAt, _expiryDate);

        // Assert
        action.Should().Throw<DomainException>().WithMessage("Email confirmation tokens require the new email address as payload data.");
    }

    #endregion

    #region State Checks (Time/Lifecycle logic)

    [Fact]
    public void IsExpired_WhenCurrentTimeIsBeforeExpiry_ShouldReturnFalse()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);
        var now = _expiryDate.AddDays(-1);

        // Act
        var isExpired = token.IsExpired(now);

        // Assert
        isExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenCurrentTimeIsExactlyAtExpiry_ShouldReturnTrue()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);
        var now = _expiryDate;

        // Act
        var isExpired = token.IsExpired(now);

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenCurrentTimeIsAfterExpiry_ShouldReturnTrue()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);
        var now = _expiryDate.AddDays(1);

        // Act
        var isExpired = token.IsExpired(now);

        // Assert
        isExpired.Should().BeTrue();
    }

    [Fact]
    public void MarkAsConsumed_ShouldSetConsumedAtToCurrentTime()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);
        var now = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);

        // Act
        token.MarkAsConsumed(now);

        // Assert
        token.ConsumedAt.Should().Be(now);
    }

    [Fact]
    public void MarkAsConsumed_WhenAlreadyConsumed_ShouldBeIdempotentAndNotOverwriteTime()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);
        var firstConsumptionTime = new DateTimeOffset(2026, 6, 2, 12, 0, 0, TimeSpan.Zero);
        var secondConsumptionTime = new DateTimeOffset(2026, 6, 3, 12, 0, 0, TimeSpan.Zero);

        // First consumption
        token.MarkAsConsumed(firstConsumptionTime);

        // Act - Attempt to consume again at a later time
        token.MarkAsConsumed(secondConsumptionTime);

        // Assert - The original time must be preserved
        token.ConsumedAt.Should().Be(firstConsumptionTime);
    }

    [Fact]
    public void IsActive_WhenNotConsumedAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);
        var now = _expiryDate.AddDays(-1); // Valid time

        // Act
        var isActive = token.IsActive(now);

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenCurrentTimeIsExactlyAtExpiry_ShouldReturnFalse()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);

        // Time hits exactly the expiration point
        var now = _expiryDate;

        // Act
        var isActive = token.IsActive(now);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenConsumedButNotExpired_ShouldReturnFalse()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);

        var consumedTime = _createdAt.AddDays(1);
        token.MarkAsConsumed(consumedTime);

        // Check time is safely BEFORE the expiration date.
        var checkTime = _expiryDate.AddDays(-1);

        // Act
        var isActive = token.IsActive(checkTime);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenNotConsumedButExpired_ShouldReturnFalse()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);
        var now = _expiryDate.AddDays(1); // After expiration

        // Act
        var isActive = token.IsActive(now);

        // Assert
        isActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenConsumedAndExpired_ShouldReturnFalse()
    {
        // Arrange
        var token = UserToken.CreateRefreshToken(_userId, _sessionId, _tokenHash, _createdAt, _expiryDate);

        var consumedTime = _createdAt.AddDays(1);
        token.MarkAsConsumed(consumedTime);

        var now = _expiryDate.AddDays(1); // After expiration

        // Act
        var isActive = token.IsActive(now);

        // Assert
        isActive.Should().BeFalse();
    }

    #endregion
}
