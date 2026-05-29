// File: src/BillingFlow.Domain/Entities/InvoiceItem.cs
using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// Represents a single line item on an invoice. 
/// This is a child entity managed exclusively by the Invoice Aggregate Root.
/// </summary>
public class InvoiceItem : Entity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    // Required by EF Core
    protected InvoiceItem() { }

    internal InvoiceItem(Guid invoiceId, string description, decimal unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Invoice item description cannot be empty.");

        if (unitPrice < 0)
            throw new DomainException("Unit price cannot be negative.");

        if (quantity <= 0)
            throw new DomainException("Quantity must be greater than zero.");

        Id = Guid.NewGuid();
        InvoiceId = invoiceId;
        Description = description;
        UnitPrice = unitPrice;
        Quantity = quantity;

        // Encapsulated calculation
        LineTotal = unitPrice * quantity;
    }
}
