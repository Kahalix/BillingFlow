using System;
using System.Collections.Generic;
using System.Linq;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Events;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// The Aggregate Root governing the invoicing and billing pipeline.
/// </summary>
public class Invoice : Entity, IAggregateRoot
{
    public Guid ClientId { get; private set; }
    public Guid? OwnerUserId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public DateTimeOffset IssueDate { get; private set; }
    public DateTimeOffset DueDate { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public byte[]? RowVersion { get; private set; }

    // Encapsulated collection to prevent external manipulation
    private readonly List<InvoiceItem> _items = new();
    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();

    protected Invoice() { }

    private Invoice(Guid clientId, Guid? ownerUserId, string invoiceNumber, DateTimeOffset issueDate, int paymentTermsDays)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        OwnerUserId = ownerUserId;
        InvoiceNumber = invoiceNumber;
        IssueDate = issueDate;
        DueDate = issueDate.AddDays(paymentTermsDays);
        Status = InvoiceStatus.Draft;
        TotalAmount = 0m;
        PaidAmount = 0m;
    }

    /// <summary>
    /// Factory method to initialize a new Draft invoice.
    /// </summary>
    public static Invoice Create(Guid clientId, Guid? ownerUserId, string invoiceNumber, DateTimeOffset issueDate, int paymentTermsDays = 14)
    {
        if (clientId == Guid.Empty)
            throw new DomainException("A valid Client ID is required to create an invoice.");

        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new DomainException("Invoice number cannot be empty.");

        if (paymentTermsDays <= 0)
            throw new DomainException("Payment terms must be at least 1 day.");

        return new Invoice(clientId, ownerUserId, invoiceNumber, issueDate, paymentTermsDays);
    }

    /// <summary>
    /// Adds a line item to the invoice and recalculates the total amount.
    /// Allowed only while the invoice is in Draft status.
    /// </summary>
    public void AddLineItem(string description, decimal unitPrice, int quantity)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("Line items can only be added to a Draft invoice.");

        var item = new InvoiceItem(Id, description, unitPrice, quantity);
        _items.Add(item);

        RecalculateTotal();
    }

    /// <summary>
    /// Locks the invoice, transitioning it from Draft to Unpaid, making it ready for client processing.
    /// Emits an event to update financial read models.
    /// </summary>
    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("Only Draft invoices can be issued.");

        if (!_items.Any())
            throw new DomainException("Cannot issue an invoice without any line items.");

        if (TotalAmount <= 0)
            throw new DomainException("Cannot issue an invoice with a zero or negative total amount.");

        Status = InvoiceStatus.Unpaid;

        AddDomainEvent(new InvoiceGeneratedEvent(Id, ClientId, TotalAmount));
    }

    /// <summary>
    /// Applies a payment amount to the invoice, updating its status dynamically based on the remaining balance.
    /// </summary>
    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Payment amount must be greater than zero.");

        if (Status == InvoiceStatus.Paid)
            throw new DomainException("This invoice is already fully paid.");

        if (Status == InvoiceStatus.Draft)
            throw new DomainException("Cannot apply payments to a Draft invoice. Issue it first.");

        var remainingBalance = TotalAmount - PaidAmount;
        if (amount > remainingBalance)
            throw new DomainException($"Payment amount ({amount}) exceeds the remaining balance ({remainingBalance}).");

        PaidAmount += amount;

        if (PaidAmount == TotalAmount)
            Status = InvoiceStatus.Paid;
        else
            Status = InvoiceStatus.PartiallyPaid;

        if (Status == InvoiceStatus.Paid)
        {
            AddDomainEvent(new InvoicePaidEvent(Id, ClientId, TotalAmount));
        }
    }

    /// <summary>
    /// Voids the invoice, making it invalid.
    /// Can only be performed on Unpaid or Draft invoices.
    /// </summary>
    public void Cancel()
    {
        if (Status == InvoiceStatus.Paid || Status == InvoiceStatus.PartiallyPaid)
            throw new DomainException("Cannot cancel an invoice that has already received payments. Issue a refund instead.");

        if (Status == InvoiceStatus.Canceled)
            throw new DomainException("This invoice is already canceled.");

        var wasIssued = Status == InvoiceStatus.Unpaid;

        Status = InvoiceStatus.Canceled;

        // If it was already issued, we must emit an event to reverse the client's debt projection
        // and detach the provided services so they can be billed again.
        if (wasIssued)
        {
            AddDomainEvent(new InvoiceCanceledEvent(Id, ClientId, TotalAmount));
        }
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(i => i.LineTotal);
    }
}
