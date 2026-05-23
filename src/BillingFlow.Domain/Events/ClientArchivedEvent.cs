// File: src/BillingFlow.Domain/Events/ClientArchivedEvent.cs
using BillingFlow.Domain.Common;

namespace BillingFlow.Domain.Events;

/// <summary>
/// Emitted when a client profile is soft-deleted.
/// Preserves the FormerUserId to ensure historical linkage is not lost 
/// when the active relationship is nullified.
/// </summary>
public record ClientArchivedEvent(Guid ClientId, Guid? FormerUserId) : IDomainEvent;
