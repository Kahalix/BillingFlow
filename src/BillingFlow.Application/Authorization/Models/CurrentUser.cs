// File: src/BillingFlow.Application/Authorization/Models/CurrentUser.cs
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Authorization.Models;

/// <summary>
/// Represents the parsed identity data of the currently authenticated user.
/// </summary>
public record CurrentUser(
    Guid Id,
    Role Role,
    Guid SessionId,
    string Email
);
