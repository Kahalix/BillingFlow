using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Identity.Queries.GetUserById;

/// <summary>
/// Contains detailed information about a specific user for administrative views.
/// </summary>
public record UserDetailsDto(
    Guid Id,
    string Email,
    Role Role,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt
);
