using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace BillingFlow.Infrastructure.BackgroundJobs;

public class PostCommitActionQueue(ILogger<PostCommitActionQueue> logger) : IPostCommitActionQueue
{
    private readonly List<Func<CancellationToken, Task>> _actions = [];

    public void Enqueue(Func<CancellationToken, Task> action)
    {
        _actions.Add(action);
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        if (_actions.Count == 0) return;

        foreach (var action in _actions)
        {
            try
            {
                await action(cancellationToken);
            }
            catch (Exception ex)
            {
                // We swallow exceptions here so a failing WebSocket connection 
                // doesn't bubble up and crash the current HTTP response.
                // The primary database transaction has already succeeded anyway.
                logger.LogWarning(ex, "A post-commit action (e.g., SignalR notification) failed during FlushAsync.");
            }
        }

        _actions.Clear();
    }
}
