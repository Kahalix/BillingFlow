using System;
using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// A passive distributed cache abstraction for session states.
/// It acts purely as a key-value store and does not orchestrate database lookups.
/// </summary>
public interface ISessionCacheService
{
    Task<bool?> GetValidationStateAsync(Guid sessionId, CancellationToken cancellationToken);
    Task SetValidationStateAsync(Guid sessionId, bool isValid, TimeSpan ttl, CancellationToken cancellationToken);
    Task InvalidateSessionAsync(Guid sessionId, CancellationToken cancellationToken);
}
