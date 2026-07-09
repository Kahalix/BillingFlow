using System;
using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Interfaces;

public interface ISessionValidationService
{
    Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken);
}
