// File: src/BillingFlow.Domain/Entities/ProvidedService.cs
using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// Represents a billable action or service performed for a client.
/// Acts as the source data for generating automated invoices.
/// </summary>
public class ProvidedService : Entity
{
    public Guid ClientId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset PerformedAt { get; private set; }

    // Tracks whether this service has been included in an invoice
    public Guid? InvoiceId { get; private set; }
    public DateTimeOffset? BilledAt { get; private set; }

    protected ProvidedService() { }

    private ProvidedService(Guid clientId, string description, decimal amount, DateTimeOffset performedAt)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        Description = description.Trim();
        Amount = amount;
        PerformedAt = performedAt;
    }

    /// <summary>
    /// Factory method enforcing invariants for a new billable service.
    /// Operates purely on primitives, keeping the Domain unaware of infrastructure like TimeProvider.
    /// </summary>
    public static ProvidedService Create(
        Guid clientId,
        string description,
        decimal amount,
        DateTimeOffset performedAt,
        DateTimeOffset now)
    {
        if (clientId == Guid.Empty)
            throw new DomainException("Client ID is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description of the provided service is required.");

        if (amount <= 0)
            throw new DomainException("Service amount must be strictly greater than zero.");

        if (performedAt > now)
            throw new DomainException("Cannot record a billable service that occurs in the future.");

        return new ProvidedService(clientId, description, amount, performedAt);
    }

    /// <summary>
    /// Links this service to an issued invoice, preventing double billing.
    /// </summary>
    public void MarkAsBilled(Guid invoiceId, DateTimeOffset billedAt)
    {
        if (InvoiceId.HasValue)
            throw new DomainException("This service has already been billed on another invoice.");

        if (invoiceId == Guid.Empty)
            throw new DomainException("A valid Invoice ID is required to mark the service as billed.");

        InvoiceId = invoiceId;
        BilledAt = billedAt;
    }
}
