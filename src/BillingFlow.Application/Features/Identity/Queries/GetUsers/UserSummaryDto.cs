// File: src/BillingFlow.Application/Features/Identity/Queries/GetCurrentUser/UserSummaryDto.cs
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Identity.Queries.GetUsers;
public record UserSummaryDto(
    Guid Id,
    string Email,
    Role Role,
    UserStatus Status,
    DateTimeOffset CreatedAt
    );

