using System;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;

using Microsoft.Extensions.Caching.Distributed;

namespace BillingFlow.Infrastructure.Identity;

public class DistributedSessionCacheService(IDistributedCache distributedCache) : ISessionCacheService
{
    private const string KeyPrefix = "session:status:";

    // Explicit byte representations for memory efficiency
    private static readonly byte[] ActivePayload = [1];
    private static readonly byte[] RevokedPayload = [0];

    public async Task<bool?> GetValidationStateAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var rawBytes = await distributedCache.GetAsync($"{KeyPrefix}{sessionId}", cancellationToken);
        if (rawBytes == null || rawBytes.Length == 0) return null;

        return rawBytes[0] == 1;
    }

    public async Task SetValidationStateAsync(Guid sessionId, bool isValid, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var payload = isValid ? ActivePayload : RevokedPayload;
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };

        await distributedCache.SetAsync($"{KeyPrefix}{sessionId}", payload, options, cancellationToken);
    }

    public async Task InvalidateSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        // Overwrite the cached state to false/revoked, ensuring instant rejection on the next read.
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };
        await distributedCache.SetAsync($"{KeyPrefix}{sessionId}", RevokedPayload, options, cancellationToken);
    }
}
