using System;

using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using FluentAssertions;

using Xunit;

namespace BillingFlow.Domain.Tests.Entities;

public class ProvidedServiceTests
{
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _invoiceId = Guid.NewGuid();
    private readonly DateTimeOffset _now = new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_ShouldInitializeInUnbilledState()
    {
        // Arrange
        var performedAt = _now.AddDays(-1);
        var description = "Consulting Services";
        var amount = 1500m;

        // Act
        var service = ProvidedService.Create(_clientId, description, amount, performedAt, _now);

        // Assert
        service.ClientId.Should().Be(_clientId);
        service.Description.Should().Be(description);
        service.Amount.Should().Be(amount);
        service.PerformedAt.Should().Be(performedAt);
        service.Status.Should().Be(ProvidedServiceStatus.Unbilled);
        service.InvoiceId.Should().BeNull();
        service.BilledAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithFutureDate_ShouldThrowDomainException()
    {
        // Arrange
        var futureDate = _now.AddDays(1);

        // Act & Assert
        Action action = () => ProvidedService.Create(_clientId, "Test", 100m, futureDate, _now);

        action.Should().Throw<DomainException>()
            .WithMessage("Cannot record a billable service that occurs in the future.");
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldThrowDomainException()
    {
        // Act & Assert
        Action action = () => ProvidedService.Create(_clientId, "Test", 0m, _now.AddDays(-1), _now);

        action.Should().Throw<DomainException>()
            .WithMessage("Service amount must be strictly greater than zero.");
    }

    [Fact]
    public void UpdateDetails_WhenUnbilled_ShouldUpdateProperties()
    {
        // Arrange
        var service = ProvidedService.Create(_clientId, "Initial", 100m, _now.AddDays(-2), _now);
        var newPerformedAt = _now.AddDays(-1);

        // Act
        service.UpdateDetails("Updated", 200m, newPerformedAt, _now);

        // Assert
        service.Description.Should().Be("Updated");
        service.Amount.Should().Be(200m);
        service.PerformedAt.Should().Be(newPerformedAt);
    }

    [Fact]
    public void UpdateDetails_WhenBilled_ShouldThrowDomainException()
    {
        // Arrange
        var service = ProvidedService.Create(_clientId, "Test", 100m, _now.AddDays(-2), _now);
        service.MarkAsBilled(_invoiceId, _now);

        // Act & Assert
        Action action = () => service.UpdateDetails("Updated", 200m, _now.AddDays(-1), _now);

        action.Should().Throw<DomainException>()
            .WithMessage($"Cannot update a service in {ProvidedServiceStatus.Billed} state.");
    }

    [Fact]
    public void Cancel_WhenUnbilled_ShouldTransitionToCanceledState()
    {
        // Arrange
        var service = ProvidedService.Create(_clientId, "Test", 100m, _now.AddDays(-2), _now);

        // Act
        service.Cancel();

        // Assert
        service.Status.Should().Be(ProvidedServiceStatus.Canceled);
    }

    [Fact]
    public void Cancel_WhenBilled_ShouldThrowDomainException()
    {
        // Arrange
        var service = ProvidedService.Create(_clientId, "Test", 100m, _now.AddDays(-2), _now);
        service.MarkAsBilled(_invoiceId, _now);

        // Act & Assert
        Action action = () => service.Cancel();

        action.Should().Throw<DomainException>()
            .WithMessage($"Cannot cancel a service that is already linked to a financial document. Issue an invoice correction instead.");
    }

    [Fact]
    public void MarkAsBilled_WhenUnbilled_ShouldSetInvoiceIdAndTransitionToBilledState()
    {
        // Arrange
        var service = ProvidedService.Create(_clientId, "Test", 100m, _now.AddDays(-2), _now);

        // Act
        service.MarkAsBilled(_invoiceId, _now);

        // Assert
        service.InvoiceId.Should().Be(_invoiceId);
        service.BilledAt.Should().Be(_now);
        service.Status.Should().Be(ProvidedServiceStatus.Billed);
    }

    [Fact]
    public void MarkAsBilled_WhenCanceled_ShouldThrowDomainException()
    {
        // Arrange
        var service = ProvidedService.Create(_clientId, "Test", 100m, _now.AddDays(-2), _now);
        service.Cancel();

        // Act & Assert
        Action action = () => service.MarkAsBilled(_invoiceId, _now);

        action.Should().Throw<DomainException>()
            .WithMessage("Only unbilled services can be attached to an invoice.");
    }
}
