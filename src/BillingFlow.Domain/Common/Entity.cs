// File: src/BillingFlow.Domain/Common/Entity.cs
namespace BillingFlow.Domain.Common;

/// <summary>
/// Base class for all entities in the domain model.
/// Provides identity management, equality comparison based on identity, and domain event tracking.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets or sets the concurrency token for optimistic locking.
    /// It is nullable because transient (unsaved) entities do not have a version yet.
    /// EF Core will automatically populate this field on every database save.
    /// </summary>
    public byte[]? RowVersion { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the read-only collection of domain events associated with this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a new domain event to the entity's internal collection.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the entity. 
    /// This should be called by the infrastructure layer (e.g., DbContext Interceptor) after events are dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current entity based strictly on its ID.
    /// Transient entities (with Empty Guids) are never considered equal to each other.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        // If it's exactly the same object in memory, it is equal.
        if (ReferenceEquals(this, other))
            return true;

        // If the types don't match, they aren't equal (avoids comparing Invoice to Client with same Guid).
        if (GetType() != other.GetType())
            return false;

        // Two transient entities (not saved to DB yet) are never considered equal.
        if (Id == Guid.Empty || other.Id == Guid.Empty)
            return false;

        return Id == other.Id;
    }

    public static bool operator ==(Entity? a, Entity? b)
    {
        if (a is null && b is null)
            return true;

        if (a is null || b is null)
            return false;

        return a.Equals(b);
    }

    public static bool operator !=(Entity? a, Entity? b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Gets a deterministic, allocation-free hash code based on the runtime type and entity ID.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }
}
