// File: tests/BillingFlow.Domain.Tests/Entities/PaymentAttemptTests.cs
using System;

using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using FluentAssertions;

using Xunit;

namespace BillingFlow.Domain.Tests.Entities;

public class PaymentAttemptTests
{
    private readonly Guid _invoiceId = Guid.NewGuid();
    private readonly decimal _amount = 1500m;
    private readonly PaymentProvider _provider = PaymentProvider.Stripe;
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    private readonly DateTimeOffset _expiresAt = DateTimeOffset.UtcNow.AddHours(24);

    [Fact]
    public void Reserve_ValidData_ShouldInitializeCorrectly()
    {
        // Act
        var attempt = PaymentAttempt.Reserve(_invoiceId, _amount, _provider, _now, _expiresAt);

        // Assert
        attempt.InvoiceId.Should().Be(_invoiceId);
        attempt.Amount.Should().Be(_amount);
        attempt.Provider.Should().Be(_provider);
        attempt.Status.Should().Be(PaymentStatus.Initializing);
        attempt.ProviderReference.Should().BeNull();
        attempt.CheckoutUrl.Should().BeNull();
    }

    [Fact]
    public void Reserve_WithBackOfficeProvider_ShouldThrowDomainException()
    {
        // Act & Assert
        Action action = () => PaymentAttempt.Reserve(_invoiceId, _amount, PaymentProvider.BackOffice, _now, _expiresAt);
        action.Should().Throw<DomainException>()
            .WithMessage("BackOffice payments do not require tracking an attempt.");
    }

    [Fact]
    public void SetCheckoutDetails_WhenInitializing_ShouldTransitionToStarted()
    {
        // Arrange
        var attempt = PaymentAttempt.Reserve(_invoiceId, _amount, _provider, _now, _expiresAt);
        var providerReference = "cs_test_12345";
        var checkoutUrl = "https://checkout.stripe.com/pay/cs_test_12345";

        // Act
        attempt.SetCheckoutDetails(providerReference, checkoutUrl);

        // Assert
        attempt.Status.Should().Be(PaymentStatus.Started);
        attempt.ProviderReference.Should().Be(providerReference);
        attempt.CheckoutUrl.Should().Be(checkoutUrl);
    }

    [Fact]
    public void SetCheckoutDetails_WhenNotInitializing_ShouldThrowDomainException()
    {
        // Arrange
        var attempt = PaymentAttempt.Reserve(_invoiceId, _amount, _provider, _now, _expiresAt);
        attempt.SetCheckoutDetails("ref1", "url1"); // Transitions to Started

        // Act & Assert
        Action action = () => attempt.SetCheckoutDetails("ref2", "url2");
        action.Should().Throw<DomainException>()
            .WithMessage("Cannot set checkout details unless the attempt is initializing.");
    }

    [Fact]
    public void MarkAsFailed_WhenSucceeded_ShouldThrowDomainException()
    {
        // Arrange
        var attempt = PaymentAttempt.Reserve(_invoiceId, _amount, _provider, _now, _expiresAt);
        attempt.MarkAsSucceeded();

        // Act & Assert
        Action action = () => attempt.MarkAsFailed("Declined");
        action.Should().Throw<DomainException>()
            .WithMessage("Cannot fail an attempt that already succeeded.");
    }

    [Fact]
    public void MarkAsFailed_ValidState_ShouldSanitizeDataAndSetStatus()
    {
        // Arrange
        var attempt = PaymentAttempt.Reserve(_invoiceId, _amount, _provider, _now, _expiresAt);
        attempt.SetCheckoutDetails("cs_test_123", "https://url");

        // Act
        attempt.MarkAsFailed("Card declined.");

        // Assert
        attempt.Status.Should().Be(PaymentStatus.Failed);
        attempt.ErrorMessage.Should().Be("Card declined.");
        attempt.CheckoutUrl.Should().BeNull(); // Sanitization check
        attempt.ProviderReference.Should().BeNull(); // Sanitization check
    }
}
