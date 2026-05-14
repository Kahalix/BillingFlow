// File: src/BillingFlow.Infrastructure/Database/Interceptors/DispatchDomainEventsInterceptor.cs
using BillingFlow.Domain.Common;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BillingFlow.Infrastructure.Database.Interceptors;

/// <summary>
/// Intercepts EF Core SaveChanges operations to automatically dispatch stored Domain Events
/// via MediatR before the database transaction completes.
/// 
/// ARCHITECTURAL NOTES & TRADE-OFFS:
/// 1. Sync-over-Async: We allow synchronous execution via .GetAwaiter().GetResult() to prevent 
///    crashing EF Core tooling, database seeders, or legacy testing frameworks that might 
///    implicitly call the synchronous SaveChanges() method. Application code must strictly use SaveChangesAsync().
/// 
/// 2. Pre-Commit Dispatching: Events are dispatched *before* the transaction commits. 
///    This guarantees that internal state changes (e.g., revoking tokens) are saved atomically.
/// 
///    BOUNDARY WARNING: Technical events and external side-effects (e.g., sending emails, 
///    calling external APIs, publishing to message brokers) SHOULD NOT be handled here. 
///    Because dispatch happens before commit, if an external call succeeds but the DB transaction 
///    subsequently fails, the system will be left in an inconsistent state.
/// 
///    FUTURE ROADMAP: For external I/O, this architecture should be augmented with the Outbox Pattern 
///    (saving events as JSON in an Outbox table and processing them post-commit via a background worker).
/// </summary>
public class DispatchDomainEventsInterceptor(IPublisher mediator) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        // Pragmatic compromise: allowing sync-over-async for tooling support.
        DispatchDomainEvents(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEvents(DbContext? context)
    {
        if (context is null) return;

        var entities = context.ChangeTracker
            .Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events immediately to prevent infinite loops during cross-handler saves.
        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        // Dispatch events using MediatR in the current request thread.
        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent);
        }
    }
}
