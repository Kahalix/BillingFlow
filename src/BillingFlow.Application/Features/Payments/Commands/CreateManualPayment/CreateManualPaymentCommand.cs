using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Payments.Commands.CreateManualPayment;

public record CreateManualPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    DateTimeOffset PaymentDate,
    string? Notes
) : ICommand<Guid>, IRequirePermission;
