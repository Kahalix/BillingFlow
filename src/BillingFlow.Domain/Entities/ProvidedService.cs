using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// Represents a billable action or service performed for a client.
/// Acts as the source data for generating automated invoices.
/// </summary>
public class ProvidedService : Entity, IAggregateRoot
{
    public Guid ClientId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTimeOffset PerformedAt { get; private set; }

    // Tracks whether this service has been included in an invoice
    public Guid? InvoiceId { get; private set; }
    public DateTimeOffset? BilledAt { get; private set; }
    public ProvidedServiceStatus Status { get; private set; }

    protected ProvidedService() { }

    private ProvidedService(Guid clientId, string description, decimal amount, DateTimeOffset performedAt)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        Description = description.Trim();
        Amount = amount;
        PerformedAt = performedAt;
        Status = ProvidedServiceStatus.Unbilled;
    }

    /// <summary>
    /// Factory method enforcing invariants for a new billable service.
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
    /// Updates the details of a provided service.
    /// Can ONLY be performed if the service has not yet been billed.
    /// </summary>
    public void UpdateDetails(string description, decimal amount, DateTimeOffset performedAt, DateTimeOffset now)
    {
        if (Status != ProvidedServiceStatus.Unbilled)
            throw new DomainException($"Cannot update a service in {Status} state.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description cannot be empty.");

        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        if (performedAt > now)
            throw new DomainException("Cannot set the performance date to the future.");

        Description = description.Trim();
        Amount = amount;
        PerformedAt = performedAt;
    }

    /// <summary>
    /// Validates if the service can be  canceled and updates its status accordingly.
    /// </summary>
    public void Cancel()
    {
        if (Status == ProvidedServiceStatus.Billed)
            throw new DomainException("Cannot cancel a service that is already linked to a financial document. Issue an invoice correction instead.");

        if (Status == ProvidedServiceStatus.Canceled) return;

        Status = ProvidedServiceStatus.Canceled;
    }

    /// <summary>
    /// Links this service to an issued invoice, preventing double billing.
    /// </summary>
    public void MarkAsBilled(Guid invoiceId, DateTimeOffset billedAt)
    {
        if (Status != ProvidedServiceStatus.Unbilled)
            throw new DomainException("Only unbilled services can be attached to an invoice.");

        if (invoiceId == Guid.Empty)
            throw new DomainException("A valid Invoice ID is required to mark the service as billed.");

        InvoiceId = invoiceId;
        BilledAt = billedAt;
        Status = ProvidedServiceStatus.Billed;
    }
}
