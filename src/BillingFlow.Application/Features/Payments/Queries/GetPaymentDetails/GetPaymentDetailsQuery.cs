using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;

namespace BillingFlow.Application.Features.Payments.Queries.GetPaymentDetails;

public record GetPaymentDetailsQuery(Guid Id) : IQuery<PaymentDetailsResponse>, IRequirePolicy;
