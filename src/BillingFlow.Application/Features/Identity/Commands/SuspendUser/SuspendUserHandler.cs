using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.SuspendUser;

public class SuspendUserHandler(IApplicationDbContext context) : IRequestHandler<SuspendUserCommand>
{
    public async Task Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        // Domain method mutates state and queues the UserSuspendedEvent internally
        user.Suspend();

        // The EF Core Interceptor will detect the queued event and dispatch it using MediatR
        await context.SaveChangesAsync(cancellationToken);
    }
}
