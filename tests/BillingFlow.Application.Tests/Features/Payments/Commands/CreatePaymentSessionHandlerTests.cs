// File: tests/BillingFlow.Application.Tests/Features/Payments/Commands/CreatePaymentSessionHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using BillingFlow.Application.Common.Exceptions;
using BillingFlow.Application.Features.Payments.Commands.CreatePaymentSession;
using BillingFlow.Application.Interfaces;
using BillingFlow.Application.Tests.Helpers;
using BillingFlow.Domain.Entities;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Exceptions;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;

using MockQueryable.Moq;

using Moq;

using Xunit;

namespace BillingFlow.Application.Tests.Features.Payments.Commands;

public class CreatePaymentSessionHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IStripeService> _stripeServiceMock;
    private readonly CreatePaymentSessionHandler _handler;

    public CreatePaymentSessionHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _stripeServiceMock = new Mock<IStripeService>();

        _handler = new CreatePaymentSessionHandler(
            _dbContextMock.Object,
            _stripeServiceMock.Object,
            TimeProvider.System,
            NullLogger<CreatePaymentSessionHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenActiveAttemptExists_ShouldReturnExistingUrlWithoutCallingStripe()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var command = new CreatePaymentSessionCommand(invoiceId, "url", "url");

        var attempt = PaymentAttempt.Reserve(invoiceId, 100m, PaymentProvider.Stripe, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        attempt.SetCheckoutDetails("cs_123", "https://checkout.stripe.com/123");

        var attempts = new List<PaymentAttempt> { attempt };
        _dbContextMock.Setup(c => c.PaymentAttempts).Returns(attempts.BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.Invoices).Returns(new List<Invoice>().BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.CheckoutUrl.Should().Be("https://checkout.stripe.com/123");
        _stripeServiceMock.Verify(s => s.CreateCheckoutSessionAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenInvoiceIsDraft_ShouldThrowForbiddenException()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var command = new CreatePaymentSessionCommand(invoiceId, "url", "url");

        var invoice = DomainTestFactory.CreateDraftInvoice(invoiceId); // Draft status

        _dbContextMock.Setup(c => c.PaymentAttempts).Returns(new List<PaymentAttempt>().BuildMockDbSet().Object);
        _dbContextMock.Setup(c => c.Invoices).Returns(new List<Invoice> { invoice }.BuildMockDbSet().Object);

        // Act & Assert
        Func<Task> action = async () => await _handler.Handle(command, CancellationToken.None);

        await action.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Payment sessions can only be created for active, unpaid invoices.");
    }
}
