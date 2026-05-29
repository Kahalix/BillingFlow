// File: src/BillingFlow.Application/Features/Clients/Queries/GetClientBalance/GetClientBalanceQuery.cs
using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Clients.Queries.GetClientBalance;

public record GetClientBalanceQuery(Guid ClientId) : IRequest<ClientBalanceReadModel>, IRequirePermission;
