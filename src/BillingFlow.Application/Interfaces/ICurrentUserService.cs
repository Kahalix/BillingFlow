using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Provides access to the currently authenticated user's details.
/// Implemented in the Infrastructure layer (reading from JWT/Claims).
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    Role UserRole { get; }
    Guid SessionId { get; }
    bool IsAuthenticated { get; }
}
