// File: src/BillingFlow.Infrastructure/BackgroundJobs/HangfireService.cs
using System.Linq.Expressions;

using Hangfire;

namespace BillingFlow.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire-specific implementation of the background job client.
/// </summary>
public class HangfireService(Hangfire.IBackgroundJobClient hangfireClient) : Application.Interfaces.IBackgroundJobClient
{
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        // Forwards the async call to the native Hangfire client.
        // Hangfire will automatically await the Task when executing the job in the background.
        return hangfireClient.Enqueue(methodCall);
    }
}
