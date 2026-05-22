// File: src/BillingFlow.Domain/Entities/Client.cs
using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Events;
using BillingFlow.Domain.Exceptions;
using BillingFlow.Domain.ValueObjects;

namespace BillingFlow.Domain.Entities;

/// <summary>
/// The primary Aggregate Root for the billing domain.
/// Represents a company or individual that receives invoices and manages business transactions.
/// </summary>
public class Client : Entity, IAggregateRoot
{
    // The AppUser ID who owns this client profile (1:1 mapping relation boundary).
    public Guid? UserId { get; private set; }
    public string CompanyName { get; private set; } = string.Empty;
    public string TaxId { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public ClientStatus Status { get; private set; }

    // Explicit concurrency token for this Aggregate Root
    public byte[]? RowVersion { get; private set; }

    // Required by EF Core
    protected Client() { }

    private Client(Guid? userId, string companyName, string taxId, Address address)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CompanyName = companyName.Trim();
        TaxId = taxId.Trim();
        Address = address;
        Status = ClientStatus.Active;
    }

    /// <summary>
    /// Factory method enforcing enterprise invariants to create a new active client.
    /// </summary>
    public static Client Create(Guid? userId, string companyName, string taxId, Address address)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User ID cannot be empty.");

        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainException("Company name is required.");

        if (string.IsNullOrWhiteSpace(taxId))
            throw new DomainException("Tax ID is required for billing initialization.");

        if (address is null)
            throw new DomainException("A complete physical billing address is required.");

        return new Client(userId, companyName, taxId, address);
    }

    /// <summary>
    /// Suspends the client, blocking financial pipeline processing like invoice generation.
    /// </summary>
    public void Suspend()
    {
        if (Status == ClientStatus.Suspended) return;

        Status = ClientStatus.Suspended;
        AddDomainEvent(new ClientSuspendedEvent(Id));
    }

    /// <summary>
    /// Re-activates a previously suspended client back into active operations.
    /// </summary>
    public void Activate()
    {
        if (Status == ClientStatus.Active) return;

        Status = ClientStatus.Active;
    }
}
