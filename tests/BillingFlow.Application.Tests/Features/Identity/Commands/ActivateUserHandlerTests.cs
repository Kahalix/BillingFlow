using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
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
    public async Task Handle_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ActivateUserCommand(userId);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser>().BuildMockDbSet().Object);

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found.");

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
        await _handler.Handle(command, CancellationToken.None);

        // Assert
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
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Status.Should().Be(UserStatus.Active);

        user.DomainEvents.Should().BeEmpty();

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
