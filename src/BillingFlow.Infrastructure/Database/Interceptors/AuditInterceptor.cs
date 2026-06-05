using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Common; // To detect Entity base class
using BillingFlow.Infrastructure.Auditing; // New namespace for AuditLog

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BillingFlow.Infrastructure.Database.Interceptors;

public class AuditInterceptor(IAuditContext auditContext, TimeProvider timeProvider) : SaveChangesInterceptor
{
    private static readonly HashSet<string> SensitiveProperties = ["PasswordHash", "TokenHash"];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ProcessAudits(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ProcessAudits(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ProcessAudits(DbContext? context)
    {
        if (context == null) return;

        // Ensure we only process domain entities, and NEVER our own infrastructure logs
        var trackedEntries = context.ChangeTracker.Entries<Entity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (!trackedEntries.Any()) return;

        var auditLogs = new List<AuditLog>();
        var now = timeProvider.GetUtcNow();

        foreach (var entry in trackedEntries)
        {
            var entityName = entry.Metadata.ClrType.Name;
            var entityId = entry.Entity.Id.ToString();
            var action = entry.State.ToString();

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary) continue;

                var propertyName = property.Metadata.Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propertyName] = ScrapeValue(propertyName, property.CurrentValue);
                        break;
                    case EntityState.Deleted:
                        oldValues[propertyName] = ScrapeValue(propertyName, property.OriginalValue);
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            oldValues[propertyName] = ScrapeValue(propertyName, property.OriginalValue);
                            newValues[propertyName] = ScrapeValue(propertyName, property.CurrentValue);
                        }
                        break;
                }
            }

            if (oldValues.Any() || newValues.Any())
            {
                var log = new AuditLog(
                    entityName: entityName,
                    entityId: entityId,
                    action: action,
                    userId: auditContext.UserId,
                    traceId: auditContext.TraceId,
                    ipAddress: auditContext.IpAddress,
                    userAgent: auditContext.UserAgent,
                    httpMethod: auditContext.HttpMethod,
                    requestPath: auditContext.RequestPath,
                    oldValues: oldValues.Any() ? JsonSerializer.Serialize(oldValues) : null,
                    newValues: newValues.Any() ? JsonSerializer.Serialize(newValues) : null,
                    timestamp: now);

                auditLogs.Add(log);
            }
        }

        context.Set<AuditLog>().AddRange(auditLogs);
    }

    private static object? ScrapeValue(string propertyName, object? value)
    {
        return SensitiveProperties.Contains(propertyName) ? "*** REDACTED ***" : value;
    }
}
