using System.Linq.Expressions;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Abstraction for enqueuing fire-and-forget background jobs.
/// Decouples the Application layer from specific implementations like Hangfire.
/// </summary>
public interface IBackgroundJobClient
{
    /// <summary>
    /// Enqueues an asynchronous job to be executed immediately in the background.
    /// </summary>
    /// <typeparam name="T">The type of the service that will execute the job.</typeparam>
    /// <param name="methodCall">An expression representing the asynchronous method call to execute.</param>
    /// <returns>The unique identifier of the scheduled job.</returns>
    string Enqueue<T>(Expression<Func<T, Task>> methodCall);
}
