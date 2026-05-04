using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;
using static StrongTypes.Api.IntegrationTests.Tests.BindingTestAsserts;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies that <see cref="NonEmptyEnumerable{T}"/> and <see cref="Maybe{T}"/>
/// bind from every non-body source via the model binders shipped in
/// <c>Kalicz.StrongTypes.AspNetCore</c>, and that an empty
/// <c>NonEmptyEnumerable</c> source produces a 400 with
/// <c>ValidationProblemDetails</c>. Body round-trip is covered too — the
/// existing JSON converters handle that path; we just confirm the binders
/// don't break it.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class NonEmptyEnumerableMaybeBindingTests(TestWebApplicationFactory factory) : IDisposable
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _client.Dispose();

    // ── Query ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Query_MultipleIds_BindsAllValues()
    {
        var response = await _client.GetAsync("/binding-probe/query-nee?ids=1&ids=2&ids=3", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([1, 2, 3], json.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal(JsonValueKind.Null, json.GetProperty("filter").ValueKind);
    }

    [Fact]
    public async Task Query_SingleId_BindsAsOneElementSequence()
    {
        var response = await _client.GetAsync("/binding-probe/query-nee?ids=42", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([42], json.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Fact]
    public async Task Query_MaybePresent_BindsToSome()
    {
        var response = await _client.GetAsync("/binding-probe/query-nee?ids=1&filter=99", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(99, json.GetProperty("filter").GetInt32());
    }

    [Fact]
    public async Task Query_MaybeOmitted_BindsToNone()
    {
        var response = await _client.GetAsync("/binding-probe/query-nee?ids=1", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("filter").ValueKind);
    }

    [Fact]
    public async Task Query_MaybeInvalid_Returns400()
    {
        var response = await _client.GetAsync("/binding-probe/query-nee?ids=1&filter=not-a-number", Ct);
        await AssertValidationProblem(response, "filter");
    }

    [Fact]
    public async Task Query_NoIds_Returns400ProblemDetails()
    {
        var response = await _client.GetAsync("/binding-probe/query-nee", Ct);
        await AssertValidationProblem(response, "ids");
    }

    [Fact]
    public async Task Query_OneIdNotANumber_Returns400()
    {
        var response = await _client.GetAsync("/binding-probe/query-nee?ids=1&ids=oops", Ct);
        await AssertValidationProblem(response, "ids");
    }

    // ── Route ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Route_SingleSegment_BindsAsOneElementSequence()
    {
        var response = await _client.GetAsync("/binding-probe/route-nee/7", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([7], json.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()));
    }

    [Fact]
    public async Task Route_NonIntegerSegment_Returns400()
    {
        var response = await _client.GetAsync("/binding-probe/route-nee/oops", Ct);
        await AssertValidationProblem(response, "ids");
    }

    // ── Header ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Header_MultipleValuesAcrossHeaders_BindsAll()
    {
        // ASP.NET Core's header binder reads StringValues from a single header
        // line as a comma-separated list when used with array binding.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header-nee");
        request.Headers.Add("X-Ids", "10");
        request.Headers.Add("X-Ids", "20");
        request.Headers.Add("X-Filter", "5");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        var ids = json.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        Assert.Contains(10, ids);
        Assert.Contains(20, ids);
        Assert.Equal(5, json.GetProperty("filter").GetInt32());
    }

    [Fact]
    public async Task Header_MissingIds_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header-nee");
        var response = await _client.SendAsync(request, Ct);
        await AssertValidationProblem(response, "X-Ids");
    }

    [Fact]
    public async Task Header_FilterMissing_BindsToNone()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header-nee");
        request.Headers.Add("X-Ids", "1");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("filter").ValueKind);
    }

    [Fact]
    public async Task Header_FilterInvalid_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header-nee");
        request.Headers.Add("X-Ids", "1");
        request.Headers.Add("X-Filter", "not-a-number");

        var response = await _client.SendAsync(request, Ct);
        await AssertValidationProblem(response, "X-Filter");
    }

    // ── Form ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Form_MultipleIds_BindsAllValues()
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("Ids", "1"),
            new("Ids", "2"),
            new("Ids", "3"),
            new("Filter", "9"),
        };
        using var content = new FormUrlEncodedContent(pairs);
        var response = await _client.PostAsync("/binding-probe/form-nee", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([1, 2, 3], json.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal(9, json.GetProperty("filter").GetInt32());
    }

    [Fact]
    public async Task Form_NoIds_Returns400()
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("Filter", "9"),
        };
        using var content = new FormUrlEncodedContent(pairs);
        var response = await _client.PostAsync("/binding-probe/form-nee", content, Ct);

        await AssertValidationProblem(response, "Ids");
    }

    [Fact]
    public async Task Form_FilterOmitted_BindsToNone()
    {
        var pairs = new List<KeyValuePair<string, string>>
        {
            new("Ids", "1"),
        };
        using var content = new FormUrlEncodedContent(pairs);
        var response = await _client.PostAsync("/binding-probe/form-nee", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("filter").ValueKind);
    }

    // ── Body (JSON converter — not the binder, but confirm it still works) ─

    [Fact]
    public async Task Body_JsonRoundTripStillWorks()
    {
        var payload = new { Ids = new[] { 1, 2, 3 }, Filter = new { Value = 7 } };
        var response = await _client.PostAsJsonAsync("/binding-probe/body-nee", payload, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal([1, 2, 3], json.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()));
        Assert.Equal(7, json.GetProperty("filter").GetInt32());
    }

    [Fact]
    public async Task Body_EmptyIdsArray_Returns400()
    {
        var payload = new { Ids = Array.Empty<int>(), Filter = new { Value = (int?)null } };
        var response = await _client.PostAsJsonAsync("/binding-probe/body-nee", payload, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
