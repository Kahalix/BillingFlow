using BillingFlow.Application.Common.Results;

using MediatR;

namespace BillingFlow.Application.Common.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
