using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Features.Identity.Commands.RefreshSession;
using BillingFlow.Application.Identity;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Identity.Commands;

public class RefreshSessionHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly Mock<ITokenHashService> _tokenHashServiceMock;
    private readonly Mock<ISessionCacheService> _sessionCacheServiceMock;
    private readonly FakeTimeProvider _timeProvider;
    private readonly IdentityOptions _identityOptions;
    private readonly RefreshSessionHandler _handler;

    private readonly DateTimeOffset _testStartTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    public RefreshSessionHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>(MockBehavior.Strict);
        _tokenHashServiceMock = new Mock<ITokenHashService>(MockBehavior.Strict);
        _sessionCacheServiceMock = new Mock<ISessionCacheService>(MockBehavior.Strict);

        _timeProvider = new FakeTimeProvider();
        _timeProvider.SetUtcNow(_testStartTime);

        _identityOptions = new IdentityOptions
        {
            SessionLifetime = TimeSpan.FromDays(7),
            AbsoluteSessionLifetime = TimeSpan.FromDays(30),
            RefreshTokenLifetime = TimeSpan.FromDays(7)
        };

        var optionsMock = Options.Create(_identityOptions);

        _handler = new RefreshSessionHandler(
            _dbContextMock.Object,
            _tokenGeneratorMock.Object,
            _tokenHashServiceMock.Object,
            _sessionCacheServiceMock.Object,
            optionsMock,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenTokenDoesNotExist_ShouldReturnInvalidRefreshTokenError()
    {
        // Arrange
        var command = new RefreshSessionCommand("invalid_raw_token");
        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns("hashed_invalid_token");
        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken>().BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Auth.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_WhenSessionIsMissing_ShouldReturnSuspendedError()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";

        var user = DomainTestFactory.CreateActiveUser();
        var token = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: Guid.NewGuid(), // Orphaned session ID
            isConsumed: false,
            hash: hashedToken,
            createdAt: _testStartTime);

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);

        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken> { token }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession>().BuildMockDbSet().Object); // Missing session
        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Users.Suspended);
    }

    [Fact]
    public async Task Handle_WhenUserIsSuspended_ShouldReturnSuspendedError()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";

        var user = DomainTestFactory.CreateActiveUser();
        user.Suspend(); // Mutate state to Suspended

        var session = DomainTestFactory.CreateUserSession(user.Id, createdAt: _testStartTime);
        var token = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: session.Id,
            isConsumed: false,
            hash: hashedToken,
            createdAt: _testStartTime);

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);

        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken> { token }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Users.Suspended);
    }

    [Fact]
    public async Task Handle_WhenTokenIsAlreadyConsumed_ShouldTriggerReplayMitigationAndReturnCompromisedError()
    {
        // Arrange
        var command = new RefreshSessionCommand("consumed_raw_token");
        var hashedToken = "hashed_consumed_token";

        var user = DomainTestFactory.CreateActiveUser();
        var session = DomainTestFactory.CreateUserSession(user.Id, createdAt: _testStartTime);

        // Create a token that has already been consumed (Replay Attack trigger)
        var consumedToken = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: session.Id,
            isConsumed: true,
            hash: hashedToken,
            createdAt: _testStartTime);

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _sessionCacheServiceMock.Setup(x => x.InvalidateSessionAsync(session.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken> { consumedToken }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Auth.SessionCompromised);

        // Verify session revocation and cache eviction
        session.IsActive(_timeProvider.GetUtcNow()).Should().BeFalse();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sessionCacheServiceMock.Verify(c => c.InvalidateSessionAsync(session.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenIsExpired_ShouldTriggerCompromiseAndReturnError()
    {
        // Arrange
        var command = new RefreshSessionCommand("expired_raw_token");
        var hashedToken = "hashed_expired_token";

        var user = DomainTestFactory.CreateActiveUser();
        var session = DomainTestFactory.CreateUserSession(user.Id, createdAt: _testStartTime);

        var token = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: session.Id,
            isConsumed: false,
            hash: hashedToken,
            createdAt: _testStartTime);

        // Advance time PAST the token expiration (7 days)
        _timeProvider.Advance(TimeSpan.FromDays(8));
        var now = _timeProvider.GetUtcNow();

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _sessionCacheServiceMock.Setup(x => x.InvalidateSessionAsync(session.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken> { token }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Auth.SessionCompromised);

        session.IsActive(now).Should().BeFalse();
        _sessionCacheServiceMock.Verify(x => x.InvalidateSessionAsync(session.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSessionIsExpired_ShouldReturnSessionExpiredError()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";

        var user = DomainTestFactory.CreateActiveUser();
        var session = DomainTestFactory.CreateUserSession(user.Id, createdAt: _testStartTime);

        // To isolate Session expiration logic, pass a Token that is artificially still valid
        var token = UserToken.CreateRefreshToken(user.Id, session.Id, hashedToken, _testStartTime, _testStartTime.AddDays(14));

        // Advance time past the session sliding expiration
        _timeProvider.Advance(TimeSpan.FromDays(8));

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken> { token }.BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Auth.SessionExpired);
    }

    [Fact]
    public async Task Handle_WhenDatabaseThrowsConcurrencyException_ShouldTranslateToConcurrentModificationError()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";

        var user = DomainTestFactory.CreateActiveUser();
        var session = DomainTestFactory.CreateUserSession(user.Id, createdAt: _testStartTime);

        var token = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: session.Id,
            isConsumed: false,
            hash: hashedToken,
            createdAt: _testStartTime);

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _tokenGeneratorMock.Setup(x => x.GenerateJwt(user, session.Id)).Returns("new_jwt");
        _tokenGeneratorMock.Setup(x => x.GenerateSecureToken()).Returns("new_raw_refresh");
        _tokenHashServiceMock.Setup(x => x.HashToken("new_raw_refresh")).Returns("new_hashed_refresh");

        var tokensDbSetMock = new List<UserToken> { token }.BuildMockDbSet();

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(tokensDbSetMock.Object);

        // Simulate a Race Condition where another request rotates this exact token at the same millisecond
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency conflict"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Auth.ConcurrentModification);
    }

    [Fact]
    public async Task Handle_WhenValidAndPastThrottlingThreshold_ShouldRotateTokensAndExtendSession()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";

        var user = DomainTestFactory.CreateActiveUser();
        var session = DomainTestFactory.CreateUserSession(user.Id, createdAt: _testStartTime);
        var originalExpiresAt = session.ExpiresAt;

        var token = DomainTestFactory.CreateUserToken(user.Id, session.Id, hash: hashedToken, createdAt: _testStartTime);
        var tokensDbSetMock = new List<UserToken> { token }.BuildMockDbSet();

        // Advance time past the 5-minute ActivityUpdateThreshold
        _timeProvider.Advance(TimeSpan.FromMinutes(10));
        var now = _timeProvider.GetUtcNow();

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _tokenGeneratorMock.Setup(x => x.GenerateJwt(user, session.Id)).Returns("new_jwt");
        _tokenGeneratorMock.Setup(x => x.GenerateSecureToken()).Returns("new_raw_refresh");
        _tokenHashServiceMock.Setup(x => x.HashToken("new_raw_refresh")).Returns("new_hashed_refresh");

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(tokensDbSetMock.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        token.IsActive(now).Should().BeFalse();
        session.ExpiresAt.Should().BeAfter(originalExpiresAt);
        session.LastSeenAt.Should().Be(now);

        // Explicitly verify that the newly generated token is being added to the database tracking
        tokensDbSetMock.Verify(x => x.Add(It.IsAny<UserToken>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExactlyAtActivityThreshold_ShouldRefreshSession()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";

        var user = DomainTestFactory.CreateActiveUser();
        var session = DomainTestFactory.CreateUserSession(user.Id, createdAt: _testStartTime);
        var originalExpiresAt = session.ExpiresAt;

        var token = DomainTestFactory.CreateUserToken(user.Id, session.Id, hash: hashedToken, createdAt: _testStartTime);
        var tokensDbSetMock = new List<UserToken> { token }.BuildMockDbSet();

        // Advance exactly 5 minutes (boundary test)
        _timeProvider.Advance(TimeSpan.FromMinutes(5));
        var now = _timeProvider.GetUtcNow();

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _tokenGeneratorMock.Setup(x => x.GenerateJwt(user, session.Id)).Returns("new_jwt");
        _tokenGeneratorMock.Setup(x => x.GenerateSecureToken()).Returns("new_raw_refresh");
        _tokenHashServiceMock.Setup(x => x.HashToken("new_raw_refresh")).Returns("new_hashed_refresh");

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(tokensDbSetMock.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.ExpiresAt.Should().BeAfter(originalExpiresAt);
        session.LastSeenAt.Should().Be(now);
    }

    [Fact]
    public async Task Handle_WhenValidButWithinThrottlingThreshold_ShouldRotateTokensWithoutExtendingSession()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";

        var user = DomainTestFactory.CreateActiveUser();
        var session = DomainTestFactory.CreateUserSession(user.Id, createdAt: _testStartTime);

        // Stable references to initial state
        var originalExpiresAt = session.ExpiresAt;
        var originalLastSeenAt = session.LastSeenAt;

        var token = DomainTestFactory.CreateUserToken(user.Id, session.Id, hash: hashedToken, createdAt: _testStartTime);
        var tokensDbSetMock = new List<UserToken> { token }.BuildMockDbSet();

        // Advance time by ONLY 2 minutes (Does not hit threshold)
        _timeProvider.Advance(TimeSpan.FromMinutes(2));

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _tokenGeneratorMock.Setup(x => x.GenerateJwt(user, session.Id)).Returns("new_jwt");
        _tokenGeneratorMock.Setup(x => x.GenerateSecureToken()).Returns("new_raw_refresh");
        _tokenHashServiceMock.Setup(x => x.HashToken("new_raw_refresh")).Returns("new_hashed_refresh");

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(tokensDbSetMock.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Expiration and telemetry must remain exactly the same due to throttling
        session.ExpiresAt.Should().Be(originalExpiresAt);
        session.LastSeenAt.Should().NotBeNull();
        session.LastSeenAt.Should().Be(originalLastSeenAt);
    }

    [Fact]
    public async Task Handle_WhenLastSeenAtIsNull_ShouldRefreshSession()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";

        var user = DomainTestFactory.CreateActiveUser();

        // Simulates old data schema before LastSeenAt was strictly tracked (database row with NULL)
        var session = DomainTestFactory.CreateUserSession(user.Id, createdAt: _testStartTime, simulateLegacyNullLastSeen: true);

        var token = DomainTestFactory.CreateUserToken(user.Id, session.Id, hash: hashedToken, createdAt: _testStartTime);
        var tokensDbSetMock = new List<UserToken> { token }.BuildMockDbSet();

        // Advance only 1 minute (below threshold, but null should bypass threshold logic)
        _timeProvider.Advance(TimeSpan.FromMinutes(1));
        var now = _timeProvider.GetUtcNow();

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _tokenGeneratorMock.Setup(x => x.GenerateJwt(user, session.Id)).Returns("new_jwt");
        _tokenGeneratorMock.Setup(x => x.GenerateSecureToken()).Returns("new_raw_refresh");
        _tokenHashServiceMock.Setup(x => x.HashToken("new_raw_refresh")).Returns("new_hashed_refresh");

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(tokensDbSetMock.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.LastSeenAt.Should().Be(now);
    }

    [Fact]
    public async Task Handle_WhenRefreshingNearAbsoluteCap_ShouldCapNewRefreshTokenAndSessionExpiry()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";

        var user = DomainTestFactory.CreateActiveUser();
        var absoluteExpiry = _testStartTime.AddDays(30);

        // Create a strict, legally valid session nearing its absolute limit
        var initialSlidingExpiry = _testStartTime.AddDays(29);
        var session = new UserSession(user.Id, _testStartTime, initialSlidingExpiry, absoluteExpiry);

        // Jump time to Day 28
        var day28 = _testStartTime.AddDays(28);
        _timeProvider.SetUtcNow(day28);

        var token = UserToken.CreateRefreshToken(user.Id, session.Id, hashedToken, day28, day28.AddDays(7));
        var tokensDbSetMock = new List<UserToken> { token }.BuildMockDbSet();

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _tokenGeneratorMock.Setup(x => x.GenerateJwt(user, session.Id)).Returns("new_jwt");
        _tokenGeneratorMock.Setup(x => x.GenerateSecureToken()).Returns("new_raw_refresh");
        _tokenHashServiceMock.Setup(x => x.HashToken("new_raw_refresh")).Returns("new_hashed_refresh");

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserSessions).Returns(new List<UserSession> { session }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(tokensDbSetMock.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Session RefreshActivity should try to add 7 days to Day 28 (Day 35), but cap at AbsoluteExpiry (Day 30)
        session.ExpiresAt.Should().Be(absoluteExpiry);

        // Application handler must also cap the Refresh Token to ensure it matches the hard session death
        result.Value.RefreshTokenExpiresAt.Should().Be(absoluteExpiry);
    }
}
