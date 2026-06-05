using System.Security.Claims;

using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Interfaces;

/// <summary>
/// Service responsible for mapping role names to specific permission claims.
/// </summary>
public interface IPermissionClaimsProvider
{
    IEnumerable<Claim> GetClaimsForRole(Role role);
}
