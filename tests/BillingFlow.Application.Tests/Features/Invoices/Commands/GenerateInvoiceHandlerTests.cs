// File: tests/BillingFlow.Application.Tests/Features/Invoices/Commands/GenerateInvoiceHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Invoices.Commands.GenerateInvoice;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Exceptions;

using FluentAssertions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Invoices.Commands;

public class GenerateInvoiceHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IInvoiceNumberGenerator> _invoiceNumberGeneratorMock;
    private readonly TimeProvider _timeProvider;
    private readonly GenerateInvoiceHandler _handler;

    public GenerateInvoiceHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _invoiceNumberGeneratorMock = new Mock<IInvoiceNumberGenerator>();
        _timeProvider = TimeProvider.System;

        _handler = new GenerateInvoiceHandler(
            _dbContextMock.Object,
            _invoiceNumberGeneratorMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WhenClientIsArchived_ShouldThrowForbiddenException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var command = new GenerateInvoiceCommand(clientId, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);

        var archivedClient = DomainTestFactory.CreateArchivedClient(id: clientId);

        _dbContextMock.Setup(c => c.Clients)
            .Returns(new List<Client> { archivedClient }.BuildMockDbSet().Object);

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Cannot generate invoices for suspended or archived billing profiles.");
    }

    [Fact]
    public async Task Handle_WhenNoUnbilledServices_ShouldThrowDomainException()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var command = new GenerateInvoiceCommand(clientId, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);

        var activeClient = DomainTestFactory.CreateActiveClient(id: clientId);

        _dbContextMock.Setup(c => c.Clients)
            .Returns(new List<Client> { activeClient }.BuildMockDbSet().Object);

        // Empty collection of provided services
        _dbContextMock.Setup(c => c.ProvidedServices)
            .Returns(new List<ProvidedService>().BuildMockDbSet().Object);

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("No unbilled provided services found for the specified period.");
    }
}
