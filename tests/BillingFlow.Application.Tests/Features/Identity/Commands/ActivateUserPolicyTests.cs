using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Features.Identity.Commands.ActivateUser;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using FluentAssertions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Identity.Commands;

public class ActivateUserPolicyTests
{
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly ActivateUserPolicy _policy;

    public ActivateUserPolicyTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _policy = new ActivateUserPolicy(_currentUserServiceMock.Object, _dbContextMock.Object);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserTriesToActivateSelf_ShouldReturnFalse()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.UserId).Returns(currentUserId);

        var command = new ActivateUserCommand(currentUserId);

        // Act
        var result = await _policy.AuthorizeAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUserCannotManageTargetRole_ShouldReturnFalse()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _currentUserServiceMock.Setup(c => c.UserId).Returns(currentUserId);
        _currentUserServiceMock.Setup(c => c.UserRole).Returns(Role.Employee); // Employee cannot manage Accountant

        var command = new ActivateUserCommand(targetUserId);

        var targetUser = DomainTestFactory.CreateActiveUser(role: Role.Accountant, id: targetUserId);
        var usersDbSetMock = new List<AppUser> { targetUser }.BuildMockDbSet();
        _dbContextMock.Setup(c => c.Users).Returns(usersDbSetMock.Object);

        // Act
        var result = await _policy.AuthorizeAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenTargetDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _currentUserServiceMock.Setup(c => c.UserId).Returns(currentUserId);
        _currentUserServiceMock.Setup(c => c.UserRole).Returns(Role.Admin);

        var command = new ActivateUserCommand(targetUserId);

        _dbContextMock.Setup(c => c.Users).Returns(new List<AppUser>().BuildMockDbSet().Object);

        // Act
        var result = await _policy.AuthorizeAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizeAsync_WhenHierarchyIsValid_ShouldReturnTrue()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        _currentUserServiceMock.Setup(c => c.UserId).Returns(currentUserId);
        _currentUserServiceMock.Setup(c => c.UserRole).Returns(Role.Manager);

        var command = new ActivateUserCommand(targetUserId);

        var targetUser = DomainTestFactory.CreateActiveUser(role: Role.Customer, id: targetUserId); // Manager CAN manage Customer
        var usersDbSetMock = new List<AppUser> { targetUser }.BuildMockDbSet();
        _dbContextMock.Setup(c => c.Users).Returns(usersDbSetMock.Object);

        // Act
        var result = await _policy.AuthorizeAsync(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }
}
