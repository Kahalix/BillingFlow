using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Common;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace BillingFlow.Infrastructure.Database.Interceptors;

/// <summary>
/// Intercepts EF Core SaveChanges operations to automatically dispatch stored Domain Events
/// via MediatR before SaveChanges completes.
/// 
/// ARCHITECTURAL NOTES & TRADE-OFFS:
/// 1. Sync-over-Async: We allow synchronous execution via .GetAwaiter().GetResult() to prevent 
///    crashing EF Core tooling, database seeders, or legacy testing frameworks that might 
///    implicitly call the synchronous SaveChanges() method. Application code must strictly use SaveChangesAsync().
/// 
/// 2. Pre-Commit Dispatching: Events are dispatched before SaveChanges completes. 
///    This is intended to keep internal state changes atomic when the persistence step succeeds.
/// 
///    BOUNDARY WARNING: Technical events and external side-effects (e.g., sending emails, 
///    calling external APIs, publishing to message brokers) SHOULD NOT be handled here. 
///    Because dispatch happens before commit, if an external call succeeds but the DB transaction 
///    subsequently fails, the system will be left in an inconsistent state.
/// 
/// 3. IMPLEMENTED ARCHITECTURE (I/O & Side-effects): 
///    To adhere to the Boundary Warning above, external I/O is handled in two distinct ways:
///    - Transactional Outbox Pattern: For durable At - Least - Once delivery(e.g., emails), events are appended to the DB.
///    - Post-SaveChanges Queue: For best-effort UI/UX updates (e.g., SignalR), actions are flushed after SaveChanges succeeds.
/// </summary>
public class DispatchDomainEventsInterceptor(
    IPublisher mediator,
    IPostCommitActionQueue postCommitQueue) : SaveChangesInterceptor
{
    // === PHASE 1: PRE-COMMIT (Domain State Mutations) ===

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

    // === PHASE 2: POST-SAVING (Best-Effort UX Notifications) ===

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        postCommitQueue.FlushAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        // Execute buffered UI notifications (e.g., SignalR).
        // If they fail, they do not affect the already-successful SaveChanges result.
        await postCommitQueue.FlushAsync(cancellationToken);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
