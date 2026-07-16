using Xunit;

namespace StrongTypes.Api.IntegrationTests.Infrastructure;

/// <summary>One shared TestWebApplicationFactory per run — the containers boot once for the whole suite.</summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<TestWebApplicationFactory>
{
    public const string Name = "Integration";
}
