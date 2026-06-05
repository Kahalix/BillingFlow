using System;

using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using FluentAssertions;

using Xunit;

namespace BillingFlow.Domain.Tests.Entities;

public class PaymentTests
{
    private readonly Guid _invoiceId = Guid.NewGuid();
    private readonly Guid _clientId = Guid.NewGuid(); // Added ClientId
    private readonly Guid _paymentAttemptId = Guid.NewGuid();
    private readonly Guid _receivedByUserId = Guid.NewGuid();
    private readonly decimal _amount = 100m;
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private readonly string _transactionId = "pi_3N23...";

    [Fact]
    public void CreateOnlinePayment_WithValidOnlineMethod_ShouldCreateSuccessfully()
    {
        // Act
        var payment = Payment.CreateOnlinePayment(
            _invoiceId, _clientId, _paymentAttemptId, _amount, PaymentProvider.Stripe, PaymentMethod.Card,
            _transactionId, _now, _now);

        // Assert
        payment.InvoiceId.Should().Be(_invoiceId);
        payment.PaymentAttemptId.Should().Be(_paymentAttemptId);
        payment.Provider.Should().Be(PaymentProvider.Stripe);
        payment.Method.Should().Be(PaymentMethod.Card);
        payment.ExternalTransactionId.Should().Be(_transactionId);
        payment.ReceivedByUserId.Should().BeNull();
    }

    [Fact]
    public void CreateOnlinePayment_WithOfflineMethod_ShouldThrowDomainException()
    {
        // Arrange
        var invalidMethodForOnline = PaymentMethod.Cash;

        // Act & Assert
        Action action = () => Payment.CreateOnlinePayment(
            _invoiceId, _clientId, _paymentAttemptId, _amount, PaymentProvider.Stripe, invalidMethodForOnline,
            _transactionId, _now, _now);

        action.Should().Throw<DomainException>()
            .WithMessage($"Payment method '{invalidMethodForOnline}' cannot be processed automatically via an online gateway.");
    }

    [Fact]
    public void CreateManualPayment_WithValidOfflineMethod_ShouldCreateSuccessfully()
    {
        // Act
        var payment = Payment.CreateManualPayment(
            _invoiceId, _clientId, _amount, PaymentMethod.BankTransfer, _now, _receivedByUserId, "Manual wiring", _now);

        // Assert
        payment.InvoiceId.Should().Be(_invoiceId);
        payment.PaymentAttemptId.Should().BeNull();
        payment.Provider.Should().Be(PaymentProvider.BackOffice);
        payment.Method.Should().Be(PaymentMethod.BankTransfer);
        payment.ExternalTransactionId.Should().BeNull();
        payment.ReceivedByUserId.Should().Be(_receivedByUserId);
        payment.Notes.Should().Be("Manual wiring");
    }

    [Fact]
    public void CreateManualPayment_WithOnlineMethod_ShouldThrowDomainException()
    {
        // Arrange
        var invalidMethodForManual = PaymentMethod.Blik;

        // Act & Assert
        Action action = () => Payment.CreateManualPayment(
            _invoiceId, _clientId, _amount, invalidMethodForManual, _now, _receivedByUserId, null, _now);

        action.Should().Throw<DomainException>()
            .WithMessage($"Payment method '{invalidMethodForManual}' requires an online gateway and cannot be created manually.");
    }
}
