using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;

public record ChangeUserRoleRequest(Role NewRole);
