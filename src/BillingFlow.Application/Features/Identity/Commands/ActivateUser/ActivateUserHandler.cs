using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Features.Identity.Commands.ActivateUser;

public class ActivateUserHandler(IApplicationDbContext context) : ICommandHandler<ActivateUserCommand>
{
    public async Task<Result> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(Errors.Users.NotFound(request.UserId));
        }

        // The domain entity manages its own state and raises the UserActivatedEvent
        user.Activate();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
