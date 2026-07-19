using BillingFlow.Application.Common.Results;

using MediatR;

namespace BillingFlow.Application.Common.Messaging;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
