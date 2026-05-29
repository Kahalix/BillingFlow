// File: tests/BillingFlow.IntegrationTests/Base/SharedTestCollection.cs
using Xunit;

namespace BillingFlow.IntegrationTests.Base;

/// <summary>
/// Defines a shared test collection for xUnit.
/// All tests in this collection will share a SINGLE instance of CustomWebApplicationFactory.
/// xUnit automatically disables parallel execution for tests within the same collection,
/// guaranteeing that Respawn can wipe the database safely without race conditions.
/// </summary>
[CollectionDefinition("IntegrationTests")]
public class SharedTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // This class has no code. It is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
