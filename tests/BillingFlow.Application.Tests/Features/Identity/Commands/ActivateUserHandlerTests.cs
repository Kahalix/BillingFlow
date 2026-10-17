using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Features.Identity.Commands.ActivateUser;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Events;

using FluentAssertions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Identity.Commands;

public class ActivateUserHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly ActivateUserHandler _handler;

    public ActivateUserHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new ActivateUserHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldReturnFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ActivateUserCommand(userId);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser>().BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.Users.NotFound(userId));

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsSuspended_ShouldActivateAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ActivateUserCommand(userId);

        var user = DomainTestFactory.CreateActiveUser(id: userId);
        user.Suspend();
        user.ClearDomainEvents();

        var usersDbSetMock = new List<AppUser> { user }.BuildMockDbSet();
        _dbContextMock.Setup(c => c.Users).Returns(usersDbSetMock.Object);

        // Setup SaveChangesAsync to return 1 (success)
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);

        // Type-safe domain event assertion
        user.DomainEvents.OfType<UserActivatedEvent>().Should().ContainSingle();

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsAlreadyActive_ShouldBeIdempotentAndSaveChangesWithoutEmittingEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ActivateUserCommand(userId);

        var user = DomainTestFactory.CreateActiveUser(id: userId);
        user.ClearDomainEvents();

        var usersDbSetMock = new List<AppUser> { user }.BuildMockDbSet();
        _dbContextMock.Setup(c => c.Users).Returns(usersDbSetMock.Object);

        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);

        user.DomainEvents.Should().BeEmpty();

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
