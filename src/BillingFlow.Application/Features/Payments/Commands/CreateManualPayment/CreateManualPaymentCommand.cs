using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Domain.Enums;

using MediatR;

namespace BillingFlow.Application.Features.Payments.Commands.CreateManualPayment;

public record CreateManualPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    DateTimeOffset PaymentDate,
    string? Notes
) : IRequest<Guid>, IRequirePermission;
