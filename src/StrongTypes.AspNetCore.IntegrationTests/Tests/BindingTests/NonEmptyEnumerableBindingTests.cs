using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;
using static StrongTypes.AspNetCore.IntegrationTests.Tests.BindingTests.BindingTestAsserts;

namespace StrongTypes.AspNetCore.IntegrationTests.Tests.BindingTests;

/// <summary>
/// Verifies the MVC binders shipped in <c>Kalicz.StrongTypes.AspNetCore</c>
/// for non-empty collections of strong types across non-body binding sources.
/// </summary>
public sealed class NonEmptyEnumerableBindingTests(AspNetCoreTestApiFactory factory) : IClassFixture<AspNetCoreTestApiFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Query_StrongTypedCollections_BindAllValues()
    {
        var response = await _client.GetAsync(
            "/binding-probe/query-nee?counts=1&counts=2&tags=alpha&tags=beta&digits=3&digits=4", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([1, 2], json.GetProperty("counts").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal(["alpha", "beta"], json.GetProperty("tags").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal([3, 4], json.GetProperty("digits").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Fact]
    public async Task Query_NullableCollectionsOmitted_BindAsNull()
    {
        var response = await _client.GetAsync("/binding-probe/query-nee?counts=7", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([7], json.GetProperty("counts").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal(JsonValueKind.Null, json.GetProperty("tags").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("digits").ValueKind);
    }

    [Theory]
    [InlineData("/binding-probe/query-nee", "", "The counts field is required.")]
    [InlineData("/binding-probe/query-nee?counts=0", "counts", null)]
    [InlineData("/binding-probe/query-nee?counts=1&digits=12", "digits", null)]
    [InlineData("/binding-probe/query-nee?counts=1&tags=", "tags", null)]
    public async Task Query_InvalidCollectionInput_Returns400(string url, string expectedField, string? expectedMessage)
    {
        var response = await _client.GetAsync(url, Ct);
        await AssertValidationProblem(response, expectedField, expectedMessage);
    }

    [Fact]
    public async Task Route_StrongTypedCollection_BindsSingleSegment()
    {
        var response = await _client.GetAsync("/binding-probe/route-nee/7", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([7], json.GetProperty("counts").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("oops")]
    public async Task Route_InvalidStrongTypedCollection_Returns400(string segment)
    {
        var response = await _client.GetAsync($"/binding-probe/route-nee/{segment}", Ct);
        await AssertValidationProblem(response, "counts");
    }

    [Fact]
    public async Task Header_StrongTypedCollections_BindAllValues()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header-nee");
        request.Headers.Add("X-Counts", "10");
        request.Headers.Add("X-Counts", "20");
        request.Headers.Add("X-Tags", "red, blue");
        request.Headers.Add("X-Digits", "5");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([10, 20], json.GetProperty("counts").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal(["red", "blue"], json.GetProperty("tags").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal([5], json.GetProperty("digits").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Fact]
    public async Task Header_MissingRequiredCollection_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header-nee");
        var response = await _client.SendAsync(request, Ct);
        await AssertValidationProblem(response, "X-Counts");
    }

    [Fact]
    public async Task Header_OptionalValuesOmitted_BindAsMissingOrNull()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header-nee");
        request.Headers.Add("X-Counts", "1");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("tags").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("digits").ValueKind);
    }

    [Fact]
    public async Task Form_ExtendedBindingProbeRequest_BindsNullableCollections()
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("tags", "north"),
            new("tags", "south"),
            new("counts", "1"),
            new("counts", "2"),
            new("digits", "8"),
        };
        using var content = new FormUrlEncodedContent(pairs);

        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(["north", "south"], json.GetProperty("tags").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal([1, 2], json.GetProperty("counts").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal([8], json.GetProperty("digits").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Theory]
    [InlineData("counts", "0")]
    [InlineData("digits", "88")]
    [InlineData("tags", "")]
    public async Task Form_InvalidNullableCollection_Returns400(string field, string badValue)
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new(field, badValue),
        };
        using var content = new FormUrlEncodedContent(pairs);

        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        await AssertValidationProblem(response, field);
    }

    [Fact]
    public async Task StrongTypedEndpoint_RequiresDigitCollection()
    {
        var response = await _client.GetAsync(
            "/binding-probe/query-nee-strong?tags=alpha&counts=1&digits=4", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(["alpha"], json.GetProperty("tags").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal([1], json.GetProperty("counts").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal([4], json.GetProperty("digits").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Theory]
    [InlineData("?tags=alpha&counts=1", "", "The digits field is required.")]
    [InlineData("?tags=alpha&counts=1&digits=x", "digits", null)]
    public async Task StrongTypedEndpoint_InvalidInput_Returns400(string query, string expectedField, string? expectedMessage)
    {
        var response = await _client.GetAsync($"/binding-probe/query-nee-strong{query}", Ct);
        await AssertValidationProblem(response, expectedField, expectedMessage);
    }
}
