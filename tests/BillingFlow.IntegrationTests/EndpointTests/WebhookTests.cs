using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using BillingFlow.Application.Interfaces;
using BillingFlow.Domain.Enums;
using BillingFlow.Domain.Events;
using BillingFlow.Infrastructure.Database;
using BillingFlow.IntegrationTests.Base;

using FluentAssertions;

using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using Xunit;

namespace BillingFlow.IntegrationTests.EndpointTests;

public class WebhookTests : BaseIntegrationTest
{
    public WebhookTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task HandleStripeWebhook_WithMissingSignatureHeader_ShouldReturn400BadRequest()
    {
        var client = Factory.CreateClient();
        var content = new StringContent("{ \"id\": \"evt_123\" }", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/webhooks/stripe", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var message = await response.Content.ReadAsStringAsync();
        message.Should().Contain("Missing Stripe signature header");
    }

    [Fact]
    public async Task HandleStripeWebhook_WithInvalidSpoofedSignature_ShouldReturn400BadRequest()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Stripe-Signature", "t=123,v1=spoofed_signature");
        var content = new StringContent("{ \"type\": \"checkout.session.completed\" }", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/webhooks/stripe", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorDetails = await response.Content.ReadAsStringAsync();
        errorDetails.ToLowerInvariant().Should().Contain("invalid webhook signature");
    }

    [Fact]
    public async Task HandleStripeWebhook_WithValidMockedSignature_ShouldProcessPaymentAndSaveToDatabase()
    {
        // 1. Arrange Data
        var (_, _, invoice) = await DataFactory.CreateUserWithIssuedInvoiceAsync(invoiceAmount: 500m);
        var providerRef = "cs_test_success_123";
        await DataFactory.CreatePaymentAttemptAsync(invoice, 500m, PaymentProvider.Stripe, PaymentStatus.Started, providerRef);

        var eventId = "evt_test_valid_" + Guid.NewGuid().ToString("N")[..6];

        // 2. Mock Validator
        var mockValidator = new Mock<IStripeWebhookValidator>();
        mockValidator.Setup(v => v.ValidateAndParse(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new PaymentCompletedEventDto(eventId, providerRef, invoice.Id, PaymentMethod.Card));

        var factoryWithMockedValidator = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStripeWebhookValidator>(mockValidator.Object);
            });
        });

        var client = factoryWithMockedValidator.CreateClient();
        client.DefaultRequestHeaders.Add("Stripe-Signature", "t=123,v1=valid");
        var content = new StringContent("{ \"type\": \"checkout.session.completed\" }", Encoding.UTF8, "application/json");

        // 3. Act
        var response = await client.PostAsync("/api/webhooks/stripe", content);

        // 4. Assert HTTP Layer
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Assert Database Layer
        using var scope = factoryWithMockedValidator.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var updatedInvoice = await db.Invoices.SingleOrDefaultAsync(i => i.Id == invoice.Id);
        updatedInvoice.Should().NotBeNull();
        updatedInvoice!.Status.Should().Be(InvoiceStatus.Paid);
        updatedInvoice.PaidAmount.Should().Be(500m);

        var payment = await db.Payments.SingleOrDefaultAsync(p => p.InvoiceId == invoice.Id);
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(500m);
        payment.Provider.Should().Be(PaymentProvider.Stripe);

        var eventLog = await db.StripeEventLogs.SingleOrDefaultAsync(e => e.EventId == eventId);
        eventLog.Should().NotBeNull();

        var paymentsCount = await db.Payments.CountAsync();
        paymentsCount.Should().Be(1);

        var eventLogsCount = await db.StripeEventLogs.CountAsync();
        eventLogsCount.Should().Be(1);
    }
}
