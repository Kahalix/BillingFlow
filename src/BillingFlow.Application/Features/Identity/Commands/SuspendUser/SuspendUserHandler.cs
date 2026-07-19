using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.SuspendUser;

public class SuspendUserHandler(IApplicationDbContext context) : ICommandHandler<SuspendUserCommand>
{
    public async Task<Result> Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(Errors.Users.NotFound(request.UserId));
        }

        // Domain method mutates state and queues the UserSuspendedEvent internally
        user.Suspend();

        // The EF Core Interceptor will detect the queued event and dispatch it using MediatR
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
