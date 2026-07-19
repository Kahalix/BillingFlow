using BillingFlow.Application.Common.Errors;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Application.Common.Results;
using BillingFlow.Application.Interfaces;

using Microsoft.EntityFrameworkCore;

using System.Threading;
using System.Threading.Tasks;

namespace BillingFlow.Application.Features.Identity.Commands.ChangeUserRole;

public class ChangeUserRoleHandler(IApplicationDbContext context) : ICommandHandler<ChangeUserRoleCommand>
{
    public async Task<Result> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(Errors.Users.NotFound(request.UserId));
        }

        user.ChangeRole(request.NewRole);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
