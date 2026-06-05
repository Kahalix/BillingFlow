// File: tests/BillingFlow.IntegrationTests/EndpointTests/PaymentsEndpointsTests.cs
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Features.Payments.Commands.CreateManualPayment;
using BillingFlow.Application.Features.Payments.Queries.GetPaymentDetails;
using BillingFlow.Domain.Enums;
using BillingFlow.Infrastructure.Database;
using BillingFlow.IntegrationTests.Base;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace BillingFlow.IntegrationTests.EndpointTests;

public class PaymentsEndpointsTests : BaseIntegrationTest
{
    public PaymentsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetPaymentDetails_WhenCustomerOwnsTheInvoice_ShouldReturn200OK()
    {
        var (owner, _, invoice) = await DataFactory.CreateUserWithIssuedInvoiceAsync();
        var payment = await DataFactory.CreateManualPaymentAsync(invoice, 500m, owner.Id);

        var client = CreateAuthorizedClient("Customer", AppPermissions.PaymentsRead, owner.Id);

        var response = await client.GetAsync($"/api/payments/{payment.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<PaymentDetailsResponse>();

        details.Should().NotBeNull();
        details!.Id.Should().Be(payment.Id);
        details.InvoiceNumber.Should().Be(invoice.InvoiceNumber);
    }

    [Fact]
    public async Task GetPaymentDetails_WhenCustomerDoesNotOwnInvoice_ShouldReturnForbiddenOrNotFound()
    {
        var (owner, _, invoice) = await DataFactory.CreateUserWithIssuedInvoiceAsync();
        var payment = await DataFactory.CreateManualPaymentAsync(invoice, 500m, owner.Id);

        var maliciousClient = CreateAuthorizedClient("Customer", AppPermissions.PaymentsRead, Guid.NewGuid());
        var response = await maliciousClient.GetAsync($"/api/payments/{payment.Id}");

        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateManualPayment_WhenUserIsAdmin_ShouldCreatePaymentAndReturn201CreatedWithValidLocation()
    {
        // 1. Arrange
        var (admin, _, invoice) = await DataFactory.CreateUserWithIssuedInvoiceAsync(role: Role.Admin);
        var client = CreateAuthorizedClient("Admin", AppPermissions.PaymentsCreateManual, admin.Id);

        var request = new CreateManualPaymentRequest(
            invoice.Id,
            1000m,
            PaymentMethod.BankTransfer,
            DateTimeOffset.UtcNow,
            "Bank Wire TX123");

        // 2. Act (POST)
        var postResponse = await client.PostAsJsonAsync("/api/payments/manual", request);

        // 3. Assert (POST)
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var locationUri = postResponse.Headers.Location;
        locationUri.Should().NotBeNull();
        locationUri!.ToString().Should().Contain("/api/payments/");

        var responseBody = await postResponse.Content.ReadFromJsonAsync<PaymentCreatedResponse>();
        responseBody.Should().NotBeNull();
        responseBody!.Id.Should().NotBeEmpty();

        // 4. Assert (GET) - Follow the Location Header
        client.DefaultRequestHeaders.Remove("Test-Permissions");
        client.DefaultRequestHeaders.Add("Test-Permissions", AppPermissions.PaymentsRead);

        var getResponse = await client.GetAsync(locationUri);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await getResponse.Content.ReadFromJsonAsync<PaymentDetailsResponse>();
        details.Should().NotBeNull();
        details!.Id.Should().Be(responseBody.Id);
        details.Amount.Should().Be(1000m);

        // 5. Assert (DATABASE)
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var savedPayment = await db.Payments.FindAsync(responseBody.Id);
        savedPayment.Should().NotBeNull();
        savedPayment!.Method.Should().Be(PaymentMethod.BankTransfer);
        savedPayment.ReceivedByUserId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task CreateManualPayment_WhenUserIsCustomer_ShouldReturn403Forbidden()
    {
        var (customer, _, invoice) = await DataFactory.CreateUserWithIssuedInvoiceAsync();
        var client = CreateAuthorizedClient("Customer", AppPermissions.PaymentsCreate, customer.Id);

        var request = new CreateManualPaymentRequest(
            invoice.Id,
            100m,
            PaymentMethod.Cash,
            DateTimeOffset.UtcNow,
            "Fake payment");

        var response = await client.PostAsJsonAsync("/api/payments/manual", request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
