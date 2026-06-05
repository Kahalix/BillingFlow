using System;
using System.Net.Http;
using System.Threading.Tasks;

using Xunit;

namespace BillingFlow.IntegrationTests.Base;

/// <summary>
/// The foundational class for all Integration Tests.
/// Inherits the shared collection, ensuring sequential execution and a single DB container.
/// </summary>
[Collection("IntegrationTests")]
public abstract class BaseIntegrationTest : IAsyncLifetime
{
    protected readonly CustomWebApplicationFactory Factory;
    protected readonly TestEntityFactory DataFactory;

    protected BaseIntegrationTest(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        DataFactory = new TestEntityFactory(Factory.Services);
    }

    /// <summary>
    /// Creates an isolated HttpClient specifically configured for a given user.
    /// Prevents DefaultRequestHeaders leakage between test executions.
    /// </summary>
    protected HttpClient CreateAuthorizedClient(string role, string permissions, Guid userId)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-Role", role);
        client.DefaultRequestHeaders.Add("Test-Permissions", permissions);
        client.DefaultRequestHeaders.Add("Test-UserId", userId.ToString());
        return client;
    }

    // Called automatically by xUnit BEFORE each [Fact] executes
    public async Task InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
