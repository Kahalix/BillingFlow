using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Interfaces;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace BillingFlow.Application.Features.Identity.Commands.ActivateUser;

public class ActivateUserHandler(IApplicationDbContext context) : IRequestHandler<ActivateUserCommand>
{
    public async Task Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        // The domain entity manages its own state and raises the UserActivatedEvent
        user.Activate();

        await context.SaveChangesAsync(cancellationToken);
    }
}
