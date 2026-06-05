using System;

using BillingFlow.Domain.Common;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Events;
using BillingFlow.Domain.Exceptions;

namespace BillingFlow.Domain.Entities;

public class Payment : Entity, IAggregateRoot
{
    public Guid InvoiceId { get; private set; }

    // Links back to the Online payment attempt. Null for manual back-office payments.

    public Guid? PaymentAttemptId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTimeOffset PaymentDate { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public PaymentMethod Method { get; private set; }

    // Generalized audit field (e.g., Stripe Charge ID, PayPal Transaction ID)
    public string? ExternalTransactionId { get; private set; }

    // Audit fields for manual back-office processing
    public Guid? ReceivedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public byte[]? RowVersion { get; private set; }

    protected Payment() { }

    private Payment(
        Guid invoiceId, Guid? paymentAttemptId, decimal amount, DateTimeOffset paymentDate,
        PaymentProvider provider, PaymentMethod method, string? externalTransactionId,
        Guid? receivedByUserId, string? notes, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        InvoiceId = invoiceId;
        PaymentAttemptId = paymentAttemptId;
        Amount = amount;
        PaymentDate = paymentDate;
        Provider = provider;
        Method = method;
        ExternalTransactionId = externalTransactionId;
        ReceivedByUserId = receivedByUserId;
        Notes = notes;
        CreatedAt = createdAt;
    }

    public static Payment CreateOnlinePayment(
        Guid invoiceId, Guid clientId, Guid paymentAttemptId, decimal amount, PaymentProvider provider, PaymentMethod method,
        string externalTransactionId, DateTimeOffset paymentDate, DateTimeOffset now)
    {
        if (invoiceId == Guid.Empty) throw new DomainException("Invoice ID is required.");
        if (paymentAttemptId == Guid.Empty) throw new DomainException("Online payments must be linked to a Payment Attempt.");
        if (amount <= 0) throw new DomainException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(externalTransactionId)) throw new DomainException("External Transaction ID is required.");
        if (provider == PaymentProvider.BackOffice) throw new DomainException("Online payments cannot use the BackOffice provider.");

        // Guard: Gateways handle specific online methods
        // Declarative, OCP-compliant validation.
        if (!method.IsOnlineMethod())
            throw new DomainException($"Payment method '{method}' cannot be processed automatically via an online gateway.");

        var payment = new Payment(
            invoiceId, paymentAttemptId, amount, paymentDate, provider, method,
            externalTransactionId, null, $"Automated online payment via {provider}.", now);

        payment.AddDomainEvent(new PaymentRecordedEvent(payment.Id, invoiceId, clientId, amount));
        return payment;
    }

    public static Payment CreateManualPayment(
        Guid invoiceId, Guid clientId, decimal amount, PaymentMethod method, DateTimeOffset paymentDate,
        Guid receivedByUserId, string? notes, DateTimeOffset now)
    {
        if (invoiceId == Guid.Empty) throw new DomainException("Invoice ID is required.");
        if (amount <= 0) throw new DomainException("Payment amount must be strictly greater than zero.");
        if (receivedByUserId == Guid.Empty) throw new DomainException("Manual payments must record the receiving user.");

        // Declarative validation for back-office processing.
        if (!method.IsOfflineMethod())
            throw new DomainException($"Payment method '{method}' requires an online gateway and cannot be created manually.");

        var payment = new Payment(
            invoiceId, null, amount, paymentDate, PaymentProvider.BackOffice, method,
            null, receivedByUserId, notes, now);

        payment.AddDomainEvent(new PaymentRecordedEvent(payment.Id, invoiceId, clientId, amount));
        return payment;


    }

}
