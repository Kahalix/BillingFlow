using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientBalance;

public record GetClientBalanceQuery(Guid ClientId) : IQuery<ClientBalanceReadModel>, IRequirePermission;
