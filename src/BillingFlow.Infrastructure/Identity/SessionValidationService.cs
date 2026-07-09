using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillingFlow.Infrastructure.Identity;

/// <summary>
/// Orchestrates the cache-aside pattern for session validation.
/// Queries the distributed cache first, falling back to the database on a miss.
/// </summary>
public class SessionValidationService : ISessionValidationService
{
    private readonly ISessionCacheService _cache;
    private readonly IApplicationDbContext _dbContext;
    private readonly SessionCacheOptions _options;
    private readonly TimeProvider _timeProvider;

    public SessionValidationService(
        ISessionCacheService cache,
        IApplicationDbContext dbContext,
        IOptions<SessionCacheOptions> options,
        TimeProvider timeProvider)
    {
        _cache = cache;
        _dbContext = dbContext;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var cachedState = await _cache.GetValidationStateAsync(sessionId, cancellationToken);
        if (cachedState.HasValue)
            return cachedState.Value;

        var now = _timeProvider.GetUtcNow();

        // Fetch the exact expiration time to prevent caching beyond the session's actual lifespan
        var expiresAt = await _dbContext.UserSessions
            .Where(s => s.Id == sessionId && s.RevokedAt == null && s.ExpiresAt > now)
            .Select(s => (DateTimeOffset?)s.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Pattern matching unwraps the value safely, eliminating compiler warnings and the need for .Value
        if (expiresAt is DateTimeOffset actualExpiresAt)
        {
            // Ensure the cache TTL never exceeds the actual session expiration
            var timeUntilExpiration = actualExpiresAt - now;
            var actualTtl = timeUntilExpiration < _options.ValidationTtl
                ? timeUntilExpiration
                : _options.ValidationTtl;

            await _cache.SetValidationStateAsync(sessionId, true, actualTtl, cancellationToken);

            return true;
        }

        // Cache the invalid state to protect the DB from hammering by revoked/expired tokens.
        await _cache.SetValidationStateAsync(sessionId, false, _options.ValidationTtl, cancellationToken);

        return false;
    }
}
