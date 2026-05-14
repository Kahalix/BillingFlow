// File: src/BillingFlow.Application/Features/Identity/Queries/GetCurrentUser/CurrentUserProfileDto.cs
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Identity.Queries.GetCurrentUser;

public record CurrentUserProfileDto(
    Guid Id,
    string Email,
    Role Role,
    UserStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt
);
