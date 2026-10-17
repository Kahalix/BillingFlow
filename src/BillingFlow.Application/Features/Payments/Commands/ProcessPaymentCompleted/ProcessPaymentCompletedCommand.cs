using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Application.Common.Messaging;
using BillingFlow.Domain.Enums;

namespace BillingFlow.Application.Features.Payments.Commands.ProcessPaymentCompleted;

public record ProcessPaymentCompletedCommand(
    string EventId,
    Guid InvoiceId,
    string ProviderReference,
    PaymentMethod Method) : ICommand, IPublicRequest;
