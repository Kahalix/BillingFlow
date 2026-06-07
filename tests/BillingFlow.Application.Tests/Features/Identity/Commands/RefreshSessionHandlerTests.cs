using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Identity.Commands.RefreshSession;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Identity.Commands;

public class RefreshSessionHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock;
    private readonly Mock<ITokenHashService> _tokenHashServiceMock;
    private readonly TimeProvider _timeProvider;
    private readonly RefreshSessionHandler _handler;

    public RefreshSessionHandlerTests()
    {
        // Using Strict behavior ensures we don't have unexpected invocations during security flows
        _dbContextMock = new Mock<IApplicationDbContext>();
        _tokenGeneratorMock = new Mock<ITokenGenerator>(MockBehavior.Strict);
        _tokenHashServiceMock = new Mock<ITokenHashService>(MockBehavior.Strict);
        _timeProvider = TimeProvider.System;

        _handler = new RefreshSessionHandler(
            _dbContextMock.Object,
            _tokenGeneratorMock.Object,
            _tokenHashServiceMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenTokenDoesNotExist_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var command = new RefreshSessionCommand("invalid_raw_token");
        var hashedToken = "hashed_invalid_token";

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);

        // Setup empty database
        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken>().BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser>().BuildMockDbSet().Object);

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid refresh token.");
    }

    [Fact]
    public async Task Handle_WhenTokenIsAlreadyConsumed_ShouldTriggerReplayMitigationAndThrow()
    {
        // Arrange
        var command = new RefreshSessionCommand("consumed_raw_token");
        var hashedToken = "hashed_consumed_token";
        var sessionId = Guid.NewGuid();

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);

        // Create an active user using the Factory
        var user = DomainTestFactory.CreateActiveUser();

        // Create a token that has ALREADY been consumed (triggering the Replay Attack scenario)
        var consumedToken = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: sessionId,
            isConsumed: true,
            hash: hashedToken);

        // Create another token in the same session that is still active
        var activeSiblingToken = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: sessionId,
            isConsumed: false,
            hash: "other_hash");

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken> { consumedToken, activeSiblingToken }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Session compromise detected. All active tokens for this session have been revoked. Please log in again.");

        // SECURITY CHECK: The active sibling token MUST be revoked (ConsumedAt is populated)
        activeSiblingToken.IsActive(_timeProvider).Should().BeFalse();

        // Verify that the emergency revocation was saved to the database
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsSuspended_ShouldThrowForbiddenException()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";
        var sessionId = Guid.NewGuid();

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);

        // Create a suspended user using the Factory
        var user = DomainTestFactory.CreateActiveUser();
        user.Suspend(); // Mutate state to Suspended

        var token = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: sessionId,
            isConsumed: false,
            hash: hashedToken);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken> { token }.BuildMockDbSet().Object);

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("This account is currently suspended or deactivated.");
    }

    [Fact]
    public async Task Handle_WhenDatabaseThrowsConcurrencyException_ShouldTranslateToUnauthorizedException()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";
        var sessionId = Guid.NewGuid();

        var user = DomainTestFactory.CreateActiveUser();

        var token = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: sessionId,
            isConsumed: false,
            hash: hashedToken);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(new List<UserToken> { token }.BuildMockDbSet().Object);

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _tokenGeneratorMock.Setup(x => x.GenerateJwt(user, sessionId)).Returns("new_jwt");
        _tokenGeneratorMock.Setup(x => x.GenerateSecureToken()).Returns("new_raw_refresh");
        _tokenHashServiceMock.Setup(x => x.HashToken("new_raw_refresh")).Returns("new_hashed_refresh");

        // Simulate a Race Condition (two requests trying to refresh the same token at the exact same millisecond)
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Concurrency conflict"));

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Session compromise detected (concurrent access). Please log in again.");
    }

    [Fact]
    public async Task Handle_WhenValid_ShouldRotateTokensAndSaveChanges()
    {
        // Arrange
        var command = new RefreshSessionCommand("valid_raw_token");
        var hashedToken = "hashed_valid_token";
        var sessionId = Guid.NewGuid();

        var user = DomainTestFactory.CreateActiveUser();

        var oldToken = DomainTestFactory.CreateUserToken(
            userId: user.Id,
            sessionId: sessionId,
            isConsumed: false,
            hash: hashedToken);

        var tokensDbSetMock = new List<UserToken> { oldToken }.BuildMockDbSet();

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser> { user }.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.UserTokens).Returns(tokensDbSetMock.Object);
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _tokenHashServiceMock.Setup(x => x.HashToken(command.RefreshToken)).Returns(hashedToken);
        _tokenGeneratorMock.Setup(x => x.GenerateJwt(user, sessionId)).Returns("new_jwt");
        _tokenGeneratorMock.Setup(x => x.GenerateSecureToken()).Returns("new_raw_refresh");
        _tokenHashServiceMock.Setup(x => x.HashToken("new_raw_refresh")).Returns("new_hashed_refresh");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_jwt");
        result.RefreshToken.Should().Be("new_raw_refresh");

        // Verify the old token was consumed
        oldToken.IsActive(_timeProvider).Should().BeFalse();

        // Verify a new token was added to the DbSet
        tokensDbSetMock.Verify(m => m.Add(It.Is<UserToken>(t =>
            t.TokenHash == "new_hashed_refresh" &&
            t.SessionId == sessionId &&
            t.UserId == user.Id)), Times.Once);

        // Verify database commit
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
