using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Hangfire;

namespace BillingFlow.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire-specific implementation of the background job client.
/// </summary>
public class HangfireService(Hangfire.IBackgroundJobClient hangfireClient) : Application.Interfaces.IBackgroundJobClient
{
    // Forwards the async call to the native Hangfire client.
    // Hangfire will automatically await the Task when executing the job in the background.
    public string Enqueue<T>(Expression<Func<T, Task>> methodCall)
    {
        return hangfireClient.Enqueue(methodCall);
    }

    public string Schedule<T>(Expression<Func<T, Task>> methodCall, TimeSpan delay)
    {
        return hangfireClient.Schedule(methodCall, delay);
    }
}
