using System;
using System.Linq;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Events;
using BillingFlow.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BillingFlow.Domain.Tests.Entities;

public class InvoiceTests
{
    private readonly Guid _clientId = Guid.NewGuid();
    private readonly Guid _ownerUserId = Guid.NewGuid();
    private readonly string _invoiceNumber = "INV/2026/05/00001";
    private readonly DateTimeOffset _issueDate = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ValidData_ShouldInitializeInDraftState()
    {
        // Act
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate, 14);

        // Assert
        invoice.ClientId.Should().Be(_clientId);
        invoice.OwnerUserId.Should().Be(_ownerUserId);
        invoice.InvoiceNumber.Should().Be(_invoiceNumber);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
        invoice.TotalAmount.Should().Be(0);
        invoice.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddLineItem_WhenInDraftStatus_ShouldRecalculateTotal()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate);

        // Act
        invoice.AddLineItem("Consulting Services", 1000m, 2); // 2000
        invoice.AddLineItem("Server Hosting", 500m, 1);       // 500

        // Assert
        invoice.Items.Should().HaveCount(2);
        invoice.TotalAmount.Should().Be(2500m);
    }

    [Fact]
    public void Issue_WithLineItems_ShouldTransitionToUnpaidAndEmitEvent()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate);
        invoice.AddLineItem("Consulting", 1000m, 1);

        // Act
        invoice.Issue();

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Unpaid);

        var domainEvent = invoice.DomainEvents.SingleOrDefault(e => e is InvoiceGeneratedEvent);
        domainEvent.Should().NotBeNull();
        ((InvoiceGeneratedEvent)domainEvent!).TotalAmount.Should().Be(1000m);
    }

    [Fact]
    public void Issue_WithoutLineItems_ShouldThrowDomainException()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate);

        // Act & Assert
        Action action = () => invoice.Issue();
        action.Should().Throw<DomainException>()
            .WithMessage("Cannot issue an invoice without any line items.");
    }

    [Fact]
    public void ApplyPayment_PartialAmount_ShouldTransitionToPartiallyPaid()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate);
        invoice.AddLineItem("Consulting", 1000m, 1);
        invoice.Issue();

        // Act
        invoice.ApplyPayment(400m);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.PaidAmount.Should().Be(400m);
    }

    [Fact]
    public void ApplyPayment_FullAmount_ShouldTransitionToPaidAndEmitEvent()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate);
        invoice.AddLineItem("Consulting", 1000m, 1);
        invoice.Issue();

        // Act
        invoice.ApplyPayment(1000m);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Paid);

        var domainEvent = invoice.DomainEvents.OfType<InvoicePaidEvent>().SingleOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.TotalInvoiceAmount.Should().Be(1000m);
    }

    [Fact]
    public void Cancel_WhenUnpaid_ShouldTransitionToCanceledAndEmitEvent()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate);
        invoice.AddLineItem("Consulting", 1000m, 1);
        invoice.Issue(); // Translates to Unpaid

        // Act
        invoice.Cancel();

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Canceled);

        var domainEvent = invoice.DomainEvents.OfType<InvoiceCanceledEvent>().SingleOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.TotalAmount.Should().Be(1000m);
    }

    [Fact]
    public void Cancel_WhenPartiallyPaid_ShouldThrowDomainException()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate);
        invoice.AddLineItem("Consulting", 1000m, 1);
        invoice.Issue(); // Translates to Unpaid
        invoice.ApplyPayment(400m); // Transitions to PartiallyPaid

        // Act & Assert
        Action action = () => invoice.Cancel();
        action.Should().Throw<DomainException>()
            .WithMessage("Cannot cancel an invoice that has already received payments. Issue a refund instead.");
    }

    [Fact]
    public void MarkAsOverdue_WhenDueDateHasNotPassed_ShouldThrowDomainException()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate, 14);
        invoice.AddLineItem("Consulting", 1000m, 1);
        invoice.Issue();

        var beforeDueDate = invoice.DueDate.AddDays(-1);

        // Act & Assert
        Action action = () => invoice.MarkAsOverdue(beforeDueDate);
        action.Should().Throw<DomainException>()
            .WithMessage("Cannot mark as overdue. The due date has not yet passed.");
    }

    [Fact]
    public void MarkAsOverdue_WhenDueDatePassedAndUnpaid_ShouldChangeStatusAndEmitEvent()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate, 14);
        invoice.AddLineItem("Consulting", 1000m, 1);
        invoice.Issue();

        var afterDueDate = invoice.DueDate.AddDays(1);
        var expectedDebt = invoice.TotalAmount;

        // Act
        invoice.MarkAsOverdue(afterDueDate);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Overdue);

        var domainEvent = invoice.DomainEvents.OfType<InvoiceOverdueEvent>().SingleOrDefault();
        domainEvent.Should().NotBeNull();
        domainEvent!.InvoiceId.Should().Be(invoice.Id);
        domainEvent.AmountDue.Should().Be(expectedDebt);
    }

    [Fact]
    public void MarkAsOverdue_WhenAlreadyOverdue_ShouldBeIdempotentAndNotEmitSecondEvent()
    {
        // Arrange
        var invoice = Invoice.Create(_clientId, _ownerUserId, _invoiceNumber, _issueDate, 14);
        invoice.AddLineItem("Consulting", 1000m, 1);
        invoice.Issue();

        var afterDueDate = invoice.DueDate.AddDays(1);

        invoice.MarkAsOverdue(afterDueDate);
        invoice.ClearDomainEvents();

        // Act
        invoice.MarkAsOverdue(afterDueDate);

        // Assert
        invoice.Status.Should().Be(InvoiceStatus.Overdue);
        invoice.DomainEvents.Should().BeEmpty();
    }
}
