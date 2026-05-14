// File: src/BillingFlow.Application/Features/Identity/Queries/GetCurrentUser/GetCurrentUserHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Queries.GetCurrentUser;

public class GetCurrentUserHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetCurrentUserQuery, CurrentUserProfileDto>
{
    public async Task<CurrentUserProfileDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await context.Users
            .AsNoTracking()
            .Select(u => new CurrentUserProfileDto(
                u.Id,
                u.Email,
                u.Role,
                u.Status,
                u.CreatedAt,
                u.LastLoginAt
            ))
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Current user profile could not be found.");
        }

        return user;
    }
}
