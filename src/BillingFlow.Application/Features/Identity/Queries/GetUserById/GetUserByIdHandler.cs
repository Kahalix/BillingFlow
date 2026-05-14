// File: src/BillingFlow.Application/Features/Identity/Queries/GetUserById/GetUserByIdHandler.cs
using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Queries.GetUserById;

public class GetUserByIdHandler(IApplicationDbContext context)
    : IRequestHandler<GetUserByIdQuery, UserDetailsDto>
{
    public async Task<UserDetailsDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
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
            throw new NotFoundException("User not found.");
        }

        return user;
    }
}
