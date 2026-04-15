using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit collection definition. All test classes tagged [Collection(Name)]
/// share the same TestWebApplicationFactory instance (containers start once).
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<TestWebApplicationFactory>
{
    public const string Name = "Integration";
}
