// File: src/BillingFlow.Application/Features/Identity/Commands/RegisterUser/RegisterUserResponse.cs
namespace BillingFlow.Application.Features.Identity.Commands.RegisterUser;

/// <summary>
/// Response payload containing the unique identifier of the newly provisioned user.
/// </summary>
public record RegisterUserResponse(Guid UserId);
