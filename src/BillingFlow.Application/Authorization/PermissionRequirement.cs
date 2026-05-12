// File: src/BillingFlow.Infrastructure/Authorization/PermissionRequirement.cs
using Microsoft.AspNetCore.Authorization;

namespace BillingFlow.Infrastructure.Authorization;

/// <summary>
/// Represents an authorization requirement based on a specific system permission.
/// </summary>
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
