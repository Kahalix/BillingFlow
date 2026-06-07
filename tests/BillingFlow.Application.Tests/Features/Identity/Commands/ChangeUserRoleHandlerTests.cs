using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;
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

public class ChangeUserRoleHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly ChangeUserRoleHandler _handler;

    public ChangeUserRoleHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new ChangeUserRoleHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ChangeUserRoleCommand(userId, Role.Manager);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser>().BuildMockDbSet().Object);

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>()
            .WithMessage("User not found.");

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserExistsAndRoleIsValid_ShouldChangeRoleAndSaveChanges()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command = new ChangeUserRoleCommand(userId, Role.Accountant);

        var user = DomainTestFactory.CreateActiveUser(id: userId);

        var usersDbSetMock = new List<AppUser> { user }.BuildMockDbSet();
        _dbContextMock.Setup(c => c.Users).Returns(usersDbSetMock.Object);

        // Setup SaveChangesAsync to return 1 (success)
        _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.Role.Should().Be(Role.Accountant);

        // Type-safe domain event assertion
        user.DomainEvents.OfType<UserRoleChangedEvent>().Should().ContainSingle();

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
