using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Features.ProvidedServices.Commands.CancelProvidedService;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;

using FluentAssertions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.ProvidedServices.Commands;

public class CancelProvidedServiceHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly CancelProvidedServiceHandler _handler;

    public CancelProvidedServiceHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new CancelProvidedServiceHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_WhenServiceDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var command = new CancelProvidedServiceCommand(serviceId);

        _dbContextMock.Setup(c => c.ProvidedServices)
            .Returns(new List<ProvidedService>().BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.ProvidedServices.NotFound(serviceId));
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenServiceExists_ShouldMutateStateAndSaveChanges()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var service = DomainTestFactory.CreateUnbilledProvidedService(id: serviceId);
        var command = new CancelProvidedServiceCommand(serviceId);

        _dbContextMock.Setup(c => c.ProvidedServices)
            .Returns(new List<ProvidedService> { service }.BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        service.Status.Should().Be(ProvidedServiceStatus.Canceled);

        _dbContextMock.Verify(c => c.ProvidedServices.Remove(It.IsAny<ProvidedService>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
