// File: tests/BillingFlow.IntegrationTests/EndpointTests/InvoicesEndpointsTests.cs
using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Features.Invoices.Common.Models;
using BillingFlow.IntegrationTests.Base;

using FluentAssertions;

using Xunit;

namespace BillingFlow.IntegrationTests.EndpointTests;

public class InvoicesEndpointsTests : BaseIntegrationTest
{
    public InvoicesEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetInvoiceDetails_WhenUserLacksReadPermission_ShouldReturn403Forbidden()
    {
        // Arrange
        var (owner, _, invoice) = await DataFactory.CreateUserWithIssuedInvoiceAsync();
        var (unauthorizedUser, _) = await DataFactory.CreateUserWithClientAsync();

        // Act: Request as third-party without permissions
        var client = CreateAuthorizedClient("Customer", permissions: "", userId: unauthorizedUser.Id);
        var response = await client.GetAsync($"/api/invoices/{invoice.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetInvoiceDetails_WhenCustomerTriesToReadAnotherCustomersInvoice_ShouldReturn404Or403()
    {
        // Arrange
        var (owner, _, invoice) = await DataFactory.CreateUserWithIssuedInvoiceAsync();
        var (maliciousUser, _) = await DataFactory.CreateUserWithClientAsync();

        // Act: Request as a user who has permissions but does not own the aggregate
        var maliciousClient = CreateAuthorizedClient("Customer", AppPermissions.InvoicesRead, maliciousUser.Id);
        var response = await maliciousClient.GetAsync($"/api/invoices/{invoice.Id}");

        // Assert: IDOR attack blocked
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetInvoiceDetails_WhenCustomerOwnsInvoice_ShouldReturn200OkWithData()
    {
        // Arrange
        var owner = await DataFactory.CreateUserWithClientAsync(companyName: "My Tech");
        var invoice = await DataFactory.CreateInvoiceAsync(owner.Client, invoiceNumber: "INV-OWNED-01");

        var client = CreateAuthorizedClient("Customer", AppPermissions.InvoicesRead, owner.User.Id);

        // Act
        var response = await client.GetAsync($"/api/invoices/{invoice.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var returnedInvoice = await response.Content.ReadFromJsonAsync<InvoiceDetailsModel>();
        returnedInvoice!.InvoiceNumber.Should().Be("INV-OWNED-01");
        returnedInvoice.Client.CompanyName.Should().Be("My Tech");
    }
}
