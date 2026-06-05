using FluentValidation;

namespace BillingFlow.Application.Features.Clients.Commands.LinkClientUser;

public class LinkClientUserValidator : AbstractValidator<LinkClientUserCommand>
{
    public LinkClientUserValidator()
    {
        RuleFor(v => v.UserId).NotEmpty().WithMessage("User ID is required to link an account.");
    }
}
