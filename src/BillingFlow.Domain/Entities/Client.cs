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
    /// Updates the core structural details of the client profile.
    /// Can only be performed if the profile is not archived.
    /// </summary>
    public void UpdateDetails(string companyName, string taxId, Address address)
    {
        if (Status == ClientStatus.Archived)
            throw new DomainException("Cannot update an archived client profile.");

        if (string.IsNullOrWhiteSpace(companyName))
            throw new DomainException("Company name is required.");

        if (string.IsNullOrWhiteSpace(taxId))
            throw new DomainException("Tax ID is required.");

        if (address is null)
            throw new DomainException("A complete physical billing address is required.");

        CompanyName = companyName.Trim();
        TaxId = taxId.Trim();
        Address = address;
    }

    /// <summary>
    /// Suspends the client, blocking financial pipeline processing like invoice generation.
    /// </summary>
    public void Suspend()
    {
        if (Status == ClientStatus.Archived)
            throw new DomainException("Cannot activate an archived client directly. It must be restored first.");

        if (Status == ClientStatus.Suspended) return;

        Status = ClientStatus.Suspended;
        AddDomainEvent(new ClientSuspendedEvent(Id));
    }

    /// <summary>
    /// Re-activates a previously suspended client back into active operations.
    /// </summary>
    public void Activate()
    {
        if (Status == ClientStatus.Archived)
            throw new DomainException("Cannot activate an archived client directly. It must be restored first.");

        if (Status == ClientStatus.Active) return;

        Status = ClientStatus.Active;
    }

    /// <summary>
    /// Performs a logical soft-delete by archiving the client profile.
    /// Automatically unlinks the AppUser to prevent unique constraint conflicts,
    /// but preserves the linkage history via the emitted Domain Event.
    /// </summary>
    public void Archive()
    {
        if (Status == ClientStatus.Archived) return;

        // Capture historical linkage before nullifying
        var formerUserId = UserId;

        Status = ClientStatus.Archived;
        UserId = null;

        AddDomainEvent(new ClientArchivedEvent(Id, formerUserId));
    }

    /// <summary>
    /// Restores an archived client profile safely into a Suspended state.
    /// It requires explicit activation afterwards to prevent accidental pipeline processing.
    /// </summary>
    public void Restore()
    {
        if (Status != ClientStatus.Archived)
            throw new DomainException("Only archived client profiles can be restored.");

        // Safe default: restored clients must be explicitly reviewed and activated
        Status = ClientStatus.Suspended;
    }

    /// <summary>
    /// Links an AppUser account to this client profile.
    /// Enforces the rule that a profile can only be linked to one user at a time.
    /// </summary>
    public void LinkUser(Guid userId)
    {
        if (Status == ClientStatus.Archived)
            throw new DomainException("Cannot link a user to an archived client profile.");

        if (UserId.HasValue)
            throw new DomainException("This client profile is already linked to a user. Unlink the current user first.");

        if (userId == Guid.Empty)
            throw new DomainException("A valid User ID is required to establish a link.");

        UserId = userId;
    }

    /// <summary>
    /// Unlinks the current AppUser from this client profile.
    /// Useful for correcting administrative mistakes or freeing up a user account.
    /// </summary>
    public void UnlinkUser()
    {
        if (Status == ClientStatus.Archived)
            throw new DomainException("Cannot modify user linkage on an archived client.");

        UserId = null;
    }

}
