// File: src/BillingFlow.Application/Features/Identity/Commands/RegisterUser/RegisterUserHandler.cs
using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Entities;

using MediatR;

namespace BillingFlow.Application.Features.Identity.Commands.RegisterUser;

/// <summary>
/// Handles the provisioning of a new user. 
/// Assumes all authorization policies and input validation have already passed in the pipeline.
/// </summary>
public class RegisterUserHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
    : IRequestHandler<RegisterUserCommand, Guid>
{
    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var passwordHash = passwordHasher.HashPassword(request.Password);

        var user = new AppUser(
            email: request.Email,
            passwordHash: passwordHash,
            role: request.TargetRole,
            createdAt: timeProvider.GetUtcNow()
        );

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
