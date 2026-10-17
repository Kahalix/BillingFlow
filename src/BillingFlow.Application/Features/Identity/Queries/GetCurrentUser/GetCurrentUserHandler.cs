using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Queries.GetCurrentUser;

public class GetCurrentUserHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserProfileDto>
{
    public async Task<Result<CurrentUserProfileDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new CurrentUserProfileDto(
                u.Id,
                u.Email,
                u.Role,
                u.Status,
                u.CreatedAt,
                u.LastLoginAt
            ))
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<CurrentUserProfileDto>(Errors.Auth.InvalidSession);
        }

        return Result.Success(user);
    }
}
