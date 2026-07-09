using FluentValidation;

namespace BillingFlow.Application.Features.Identity.Commands.DashboardLogin;

public class DashboardLoginValidator : AbstractValidator<DashboardLoginCommand>
{
    public DashboardLoginValidator()
    {
        RuleFor(v => v.Email).NotEmpty().EmailAddress();
        RuleFor(v => v.Password).NotEmpty();
    }
}
