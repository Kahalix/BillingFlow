// File: tests/BillingFlow.IntegrationTests/EndpointTests/ReportsEndpointsTests.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

using BillingFlow.Application.Authorization.Permissions;
using BillingFlow.Application.Features.Reports.Queries.GetMonthlyRevenue;
using BillingFlow.Domain.Enums;
using BillingFlow.IntegrationTests.Base;

using FluentAssertions;

using Xunit;

namespace BillingFlow.IntegrationTests.EndpointTests;

public class ReportsEndpointsTests : BaseIntegrationTest
{
    public ReportsEndpointsTests(CustomWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetMonthlyRevenue_WhenUserLacksPermissions_ShouldReturn403Forbidden()
    {
        // Arrange
        var client = CreateAuthorizedClient("Customer", permissions: "", userId: Guid.NewGuid());

        // Act
        var response = await client.GetAsync("/api/reports/monthly-revenue?year=2026&month=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMonthlyRevenue_WithValidPermissions_ShouldExecuteMigratedStoredProcedure()
    {
        // Arrange
        var admin = await DataFactory.CreateUserWithClientAsync(
            companyName: "Enterprise Corp",
            role: Role.Admin);

        var targetYear = 2026;
        var targetMonth = 5;

        // Seed an invoice exactly in the reporting period
        var issueDate = new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero);
        await DataFactory.CreateIssuedInvoiceAsync(admin.Client, amount: 10000m, issueDate: issueDate);

        var client = CreateAuthorizedClient("Admin", AppPermissions.ReportsRead, admin.User.Id);

        // Act
        var response = await client.GetAsync($"/api/reports/monthly-revenue?year={targetYear}&month={targetMonth}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<List<MonthlyRevenueDto>>();
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result![0].CompanyName.Should().Be("Enterprise Corp");
        result[0].TotalBilled.Should().Be(10000m);
    }

    [Fact]
    public async Task GetMonthlyRevenue_WithoutRequiredParameters_ShouldReturn400BadRequest()
    {
        // Arrange
        var client = CreateAuthorizedClient("Admin", AppPermissions.ReportsRead, Guid.NewGuid());

        // Act
        var response = await client.GetAsync("/api/reports/monthly-revenue");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
