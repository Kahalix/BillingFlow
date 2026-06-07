using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using FluentAssertions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Identity.Commands;

public class ChangeUserRolePolicyTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly ChangeUserRolePolicy _policy;

    public ChangeUserRolePolicyTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _policy = new ChangeUserRolePolicy(_currentUserServiceMock.Object, _dbContextMock.Object);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserTriesToChangeOwnRole_ShouldReturnFalse()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.UserId).Returns(currentUserId);

        var command = new ChangeUserRoleCommand(currentUserId, Role.Manager);

        // Act
        var result = await _policy.AuthorizeAsync(command, CancellationToken.None);

        // Assert - Lockout prevention rule
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserCannotManageNewRole_ShouldReturnFalse()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _currentUserServiceMock.Setup(c => c.UserId).Returns(currentUserId);
        _currentUserServiceMock.Setup(c => c.UserRole).Returns(Role.Manager); // Manager cannot assign Admin

        var command = new ChangeUserRoleCommand(targetUserId, Role.Admin);

        // Act
        var result = await _policy.AuthorizeAsync(command, CancellationToken.None);

        // Assert - Privilege escalation rule
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserCannotManageTargetCurrentRole_ShouldReturnFalse()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _currentUserServiceMock.Setup(c => c.UserId).Returns(currentUserId);
        _currentUserServiceMock.Setup(c => c.UserRole).Returns(Role.Accountant);

        var command = new ChangeUserRoleCommand(targetUserId, Role.Customer); // Accountant CAN assign Customer...

        // ... but the target is currently a Manager (Accountant cannot manage Managers)
        var targetUser = DomainTestFactory.CreateActiveUser(role: Role.Manager, id: targetUserId);
        var usersDbSetMock = new List<AppUser> { targetUser }.BuildMockDbSet();
        _dbContextMock.Setup(c => c.Users).Returns(usersDbSetMock.Object);

        // Act
        var result = await _policy.AuthorizeAsync(command, CancellationToken.None);

        // Assert - Hierarchical modification check
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenTargetDoesNotExist_ShouldReturnTrueToAllowHandlerToThrow404()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _currentUserServiceMock.Setup(c => c.UserId).Returns(currentUserId);
        _currentUserServiceMock.Setup(c => c.UserRole).Returns(Role.Admin);

        var command = new ChangeUserRoleCommand(targetUserId, Role.Manager);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser>().BuildMockDbSet().Object);

        // Act
        var result = await _policy.AuthorizeAsync(command, CancellationToken.None);

        // Assert - Policy passes, delegating the "Not Found" error to the Handler
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenHierarchyIsValid_ShouldReturnTrue()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _currentUserServiceMock.Setup(c => c.UserId).Returns(currentUserId);
        _currentUserServiceMock.Setup(c => c.UserRole).Returns(Role.Admin); // Admin can do anything

        var command = new ChangeUserRoleCommand(targetUserId, Role.Employee);

        var targetUser = DomainTestFactory.CreateActiveUser(role: Role.Customer, id: targetUserId);
        var usersDbSetMock = new List<AppUser> { targetUser }.BuildMockDbSet();
        _dbContextMock.Setup(c => c.Users).Returns(usersDbSetMock.Object);

        // Act
        var result = await _policy.AuthorizeAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }
}
