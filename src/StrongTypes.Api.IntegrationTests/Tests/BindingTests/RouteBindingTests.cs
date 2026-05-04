using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;
using static StrongTypes.Api.IntegrationTests.Tests.BindingTestAsserts;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies that strong types round-trip through MVC's <c>[FromRoute]</c>
/// model binding. Route segments are required by definition, so only the
/// required-value path applies; missing-segment cases are 404s (route-match
/// failure) rather than 400s (binding failure).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class RouteBindingTests(TestWebApplicationFactory factory) : IDisposable
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task FromRoute_ValidValues_Binds()
    {
        var response = await _client.GetAsync("/binding-probe/route/Charlie/9/4", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Charlie", json.GetProperty("name").GetString());
        Assert.Equal(9, json.GetProperty("count").GetInt32());
        Assert.Equal(4, json.GetProperty("digit").GetInt32());
    }

    [Fact]
    public async Task FromRoute_NonPositiveCount_Returns400()
    {
        var response = await _client.GetAsync("/binding-probe/route/Charlie/0/4", Ct);
        await AssertValidationProblem(response, "count");
    }

    [Fact]
    public async Task FromRoute_InvalidDigit_Returns400()
    {
        var response = await _client.GetAsync("/binding-probe/route/Charlie/9/42", Ct);
        await AssertValidationProblem(response, "digit");
    }

    [Fact]
    public async Task FromRoute_NonIntegerCount_Returns404()
    {
        // Route constraint `:int` rejects the segment before binding gets a chance,
        // so this is a route-match failure (404), not a binding failure (400).
        var response = await _client.GetAsync("/binding-probe/route/Charlie/abc/4", Ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
