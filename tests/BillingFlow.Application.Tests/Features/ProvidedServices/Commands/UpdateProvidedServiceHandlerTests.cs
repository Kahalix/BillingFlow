using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Features.ProvidedServices.Commands.UpdateProvidedService;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Exceptions;

using FluentAssertions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.ProvidedServices.Commands;

public class UpdateProvidedServiceHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly TimeProvider _timeProvider;
    private readonly UpdateProvidedServiceHandler _handler;

    public UpdateProvidedServiceHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _timeProvider = TimeProvider.System;
        _handler = new UpdateProvidedServiceHandler(_dbContextMock.Object, _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenServiceDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var command = new UpdateProvidedServiceCommand(serviceId, "New Desc", 200m, DateTimeOffset.UtcNow);

        _dbContextMock.Setup(c => c.ProvidedServices)
            .Returns(new List<ProvidedService>().BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Errors.ProvidedServices.NotFound(serviceId));
    }

    [Fact]
    public async Task Handle_WhenServiceExistsAndUnbilled_ShouldUpdateAndSaveChanges()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var service = DomainTestFactory.CreateUnbilledProvidedService(id: serviceId);

        var newAmount = 500m;
        var command = new UpdateProvidedServiceCommand(serviceId, "Updated Scope", newAmount, DateTimeOffset.UtcNow.AddHours(-1));

        _dbContextMock.Setup(c => c.ProvidedServices)
            .Returns(new List<ProvidedService> { service }.BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        service.Amount.Should().Be(newAmount);
        service.Description.Should().Be("Updated Scope");

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenServiceIsAlreadyBilled_ShouldLetDomainExceptionBubbleUp()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var billedService = DomainTestFactory.CreateBilledProvidedService(id: serviceId);

        var command = new UpdateProvidedServiceCommand(serviceId, "Sneaky Update", 1000m, DateTimeOffset.UtcNow);

        _dbContextMock.Setup(c => c.ProvidedServices)
            .Returns(new List<ProvidedService> { billedService }.BuildMockDbSet().Object);

        // Act & Assert
        // Domain layer enforces invariants. Application layer does not catch this by design,
        // so we expect the pure DomainException to bubble up to the global exception handler.
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("Cannot update a service in Billed state.");

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
