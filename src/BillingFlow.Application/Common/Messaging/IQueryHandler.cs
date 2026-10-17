using BillingFlow.Application.Common.Results;

using MediatR;

namespace BillingFlow.Application.Common.Messaging;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
