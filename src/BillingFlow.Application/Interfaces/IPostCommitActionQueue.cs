using System;
using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// A lightweight, in-memory queue scoped to the current unit of work / request.
/// Used to buffer best-effort UI/UX notifications (like SignalR WebSockets)
/// so they are executed only AFTER SaveChanges succeeds.
/// </summary>
public interface IPostCommitActionQueue
{
    /// <summary>
    /// Buffers an asynchronous action to be executed post-commit.
    /// </summary>
    void Enqueue(Func<CancellationToken, Task> action);

    /// <summary>
    /// Executes all buffered actions sequentially. 
    /// Intended to be called immediately after SaveChangesAsync() returns a success result.
    /// </summary>
    Task FlushAsync(CancellationToken cancellationToken);
}
