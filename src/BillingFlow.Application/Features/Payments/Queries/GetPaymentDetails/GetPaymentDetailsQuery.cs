using System;

using BillingFlow.Application.Authorization.Requirements;

using MediatR;

namespace BillingFlow.Application.Features.Payments.Queries.GetPaymentDetails;

public record GetPaymentDetailsQuery(Guid Id) : IRequest<PaymentDetailsResponse>, IRequirePolicy;
