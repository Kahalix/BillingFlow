using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace BillingFlow.Application.Features.Identity.Queries.GetUserById;

public class GetUserByIdHandler(IApplicationDbContext context)
    : IQueryHandler<GetUserByIdQuery, UserDetailsDto>
{
    public async Task<Result<UserDetailsDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new UserDetailsDto(
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
            return Result.Failure<UserDetailsDto>(Errors.Users.NotFound(request.UserId));
        }

        return Result.Success(user);
    }
}
