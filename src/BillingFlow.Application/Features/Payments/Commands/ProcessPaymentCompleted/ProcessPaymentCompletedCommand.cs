// File: src/BillingFlow.Application/Features/Payments/Commands/ProcessPaymentCompleted/ProcessPaymentCompletedCommand.cs
using System;

using BillingFlow.Application.Authorization.Requirements;
using BillingFlow.Domain.Enums;

using MediatR;

namespace BillingFlow.Application.Features.Payments.Commands.ProcessPaymentCompleted;

public record ProcessPaymentCompletedCommand(
    string EventId,
    Guid InvoiceId,
    string ProviderReference,
    PaymentMethod Method) : IRequest, IPublicRequest;
