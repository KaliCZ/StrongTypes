using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies that strong types round-trip through every non-body model-binding
/// source MVC supports — query, route, header, form, and minimal-API-style
/// implicit query binding — and that invalid input lands in the
/// <see cref="HttpStatusCode.BadRequest"/> branch with a
/// <c>ValidationProblemDetails</c> payload (i.e. <c>[ApiController]</c> turned
/// the binding failure into ModelState, not a 500).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class NonBodyBindingTests(TestWebApplicationFactory factory) : IDisposable
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _client.Dispose();

    // ── [FromQuery] ──────────────────────────────────────────────────────

    [Fact]
    public async Task FromQuery_ValidValues_BindsAndEchoes()
    {
        var response = await _client.GetAsync(
            "/binding-probe/query?name=Alice&count=42&email=alice@example.com", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Alice", json.GetProperty("name").GetString());
        Assert.Equal(42, json.GetProperty("count").GetInt32());
        Assert.Equal("alice@example.com", json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task FromQuery_OptionalEmailOmitted_BindsWithNull()
    {
        var response = await _client.GetAsync("/binding-probe/query?name=Alice&count=1", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("email").ValueKind);
    }

    [Theory]
    [InlineData("name=&count=1", "name")]
    [InlineData("name=%20%20&count=1", "name")]
    [InlineData("name=Alice&count=0", "count")]
    [InlineData("name=Alice&count=-5", "count")]
    [InlineData("name=Alice&count=not-a-number", "count")]
    [InlineData("name=Alice&count=1&email=not-an-email", "email")]
    public async Task FromQuery_Invalid_Returns400ProblemDetails(string query, string expectedField)
    {
        var response = await _client.GetAsync($"/binding-probe/query?{query}", Ct);

        await AssertValidationProblem(response, expectedField);
    }

    [Fact]
    public async Task FromQuery_MissingRequired_Returns400()
    {
        var response = await _client.GetAsync("/binding-probe/query?count=1", Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Implicit query binding (no [From*] attribute) ────────────────────

    [Fact]
    public async Task ImplicitQuery_ValidValues_Binds()
    {
        var response = await _client.GetAsync("/binding-probe/query-implicit?name=Bob&count=7", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Bob", json.GetProperty("name").GetString());
        Assert.Equal(7, json.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task ImplicitQuery_Invalid_Returns400()
    {
        var response = await _client.GetAsync("/binding-probe/query-implicit?name=Bob&count=0", Ct);
        await AssertValidationProblem(response, "count");
    }

    // ── [FromRoute] ──────────────────────────────────────────────────────

    [Fact]
    public async Task FromRoute_ValidValues_Binds()
    {
        var response = await _client.GetAsync("/binding-probe/route/Charlie/9", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Charlie", json.GetProperty("name").GetString());
        Assert.Equal(9, json.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task FromRoute_NonPositiveCount_Returns400()
    {
        var response = await _client.GetAsync("/binding-probe/route/Charlie/0", Ct);
        await AssertValidationProblem(response, "count");
    }

    [Fact]
    public async Task FromRoute_NonIntegerCount_Returns404()
    {
        // Route constraint `:int` rejects the segment before binding gets a chance,
        // so this is a route-match failure (404), not a binding failure (400).
        var response = await _client.GetAsync("/binding-probe/route/Charlie/abc", Ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── [FromHeader] ─────────────────────────────────────────────────────

    [Fact]
    public async Task FromHeader_ValidValues_Binds()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "13");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Dana", json.GetProperty("name").GetString());
        Assert.Equal(13, json.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task FromHeader_InvalidCount_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "0");

        var response = await _client.SendAsync(request, Ct);

        await AssertValidationProblem(response, "X-Count");
    }

    [Fact]
    public async Task FromHeader_MissingHeader_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Count", "1");

        var response = await _client.SendAsync(request, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── [FromForm] ───────────────────────────────────────────────────────

    [Fact]
    public async Task FromForm_ValidValues_Binds()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "Eve",
            ["count"] = "21",
            ["email"] = "eve@example.com",
        });
        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Eve", json.GetProperty("name").GetString());
        Assert.Equal(21, json.GetProperty("count").GetInt32());
        Assert.Equal("eve@example.com", json.GetProperty("email").GetString());
    }

    [Fact]
    public async Task FromForm_OptionalEmailOmitted_Binds()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "Eve",
            ["count"] = "21",
        });
        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("email").ValueKind);
    }

    [Theory]
    [InlineData("", "21", null, "Name")]
    [InlineData("Eve", "0", null, "Count")]
    [InlineData("Eve", "not-a-number", null, "Count")]
    [InlineData("Eve", "21", "not-an-email", "Email")]
    public async Task FromForm_Invalid_Returns400(string name, string count, string? email, string expectedField)
    {
        var pairs = new Dictionary<string, string> { ["name"] = name, ["count"] = count };
        if (email is not null) pairs["email"] = email;
        using var content = new FormUrlEncodedContent(pairs);

        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        await AssertValidationProblem(response, expectedField);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static async Task AssertValidationProblem(HttpResponseMessage response, string expectedField)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        var errors = problem.GetProperty("errors");
        Assert.Equal(JsonValueKind.Object, errors.ValueKind);
        var found = false;
        foreach (var prop in errors.EnumerateObject())
        {
            if (string.Equals(prop.Name, expectedField, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                break;
            }
        }
        Assert.True(found, $"Expected ValidationProblemDetails to include error for '{expectedField}'. Actual: {problem}");
    }
}
