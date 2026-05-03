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
/// the binding failure into ModelState, not a 500). Each non-body source is
/// exercised with both required and nullable variants of the wrapped types so
/// the "absent → null" path is covered alongside the value path.
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

    private const string ValidQuery =
        "name=Alice&count=42&digit=7&email=alice@example.com";

    // ── [FromQuery] ──────────────────────────────────────────────────────

    [Fact]
    public async Task FromQuery_AllValuesPresent_BindsBothRequiredAndNullable()
    {
        var response = await _client.GetAsync(
            $"/binding-probe/query?{ValidQuery}&nullableName=Bob&nullableCount=2&nullableDigit=3&nullableEmail=bob@example.com", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Alice", json.GetProperty("name").GetString());
        Assert.Equal("Bob", json.GetProperty("nullableName").GetString());
        Assert.Equal(42, json.GetProperty("count").GetInt32());
        Assert.Equal(2, json.GetProperty("nullableCount").GetInt32());
        Assert.Equal(7, json.GetProperty("digit").GetInt32());
        Assert.Equal(3, json.GetProperty("nullableDigit").GetInt32());
        Assert.Equal("alice@example.com", json.GetProperty("email").GetString());
        Assert.Equal("bob@example.com", json.GetProperty("nullableEmail").GetString());
    }

    [Fact]
    public async Task FromQuery_NullablesOmitted_BindsAsNull()
    {
        var response = await _client.GetAsync($"/binding-probe/query?{ValidQuery}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableName").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableCount").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableDigit").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableEmail").ValueKind);
    }

    [Theory]
    [InlineData("name=&count=1&digit=0&email=alice@example.com", "name")]
    [InlineData("name=%20%20&count=1&digit=0&email=alice@example.com", "name")]
    [InlineData("name=Alice&count=0&digit=0&email=alice@example.com", "count")]
    [InlineData("name=Alice&count=-5&digit=0&email=alice@example.com", "count")]
    [InlineData("name=Alice&count=not-a-number&digit=0&email=alice@example.com", "count")]
    [InlineData("name=Alice&count=1&digit=ab&email=alice@example.com", "digit")]
    [InlineData("name=Alice&count=1&digit=42&email=alice@example.com", "digit")]
    [InlineData("name=Alice&count=1&digit=0&email=not-an-email", "email")]
    public async Task FromQuery_InvalidRequired_Returns400ProblemDetails(string query, string expectedField)
    {
        var response = await _client.GetAsync($"/binding-probe/query?{query}", Ct);

        await AssertValidationProblem(response, expectedField);
    }

    [Theory]
    [InlineData("nullableCount=0", "nullableCount")]
    [InlineData("nullableDigit=42", "nullableDigit")]
    [InlineData("nullableEmail=not-an-email", "nullableEmail")]
    public async Task FromQuery_NonEmptyInvalidNullable_Returns400ProblemDetails(string nullableQuery, string expectedField)
    {
        // A nullable param that's *present with a non-empty invalid value*
        // still has to fail — nullable means the slot may be omitted, not
        // that any garbage is OK.
        var response = await _client.GetAsync($"/binding-probe/query?{ValidQuery}&{nullableQuery}", Ct);

        await AssertValidationProblem(response, expectedField);
    }

    [Fact]
    public async Task FromQuery_EmptyNullable_BindsAsNull()
    {
        // MVC's NullableConverter short-circuits empty strings to null for
        // nullable types — TryParse never runs. This is framework behaviour,
        // not a strong-types behaviour: documented here so a regression
        // (e.g. a custom binder that flips it to 400) is intentional.
        var response = await _client.GetAsync($"/binding-probe/query?{ValidQuery}&nullableName=", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableName").ValueKind);
    }

    [Fact]
    public async Task FromQuery_MissingRequired_Returns400()
    {
        var response = await _client.GetAsync(
            "/binding-probe/query?count=1&digit=0&email=alice@example.com", Ct);
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

    // ── [FromRoute] (required-only — route segments are required) ────────

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

    // ── [FromHeader] ─────────────────────────────────────────────────────

    [Fact]
    public async Task FromHeader_AllValuesPresent_BindsBothRequiredAndNullable()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Nullable-Name", "Dana2");
        request.Headers.Add("X-Count", "13");
        request.Headers.Add("X-Nullable-Count", "5");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Dana", json.GetProperty("name").GetString());
        Assert.Equal("Dana2", json.GetProperty("nullableName").GetString());
        Assert.Equal(13, json.GetProperty("count").GetInt32());
        Assert.Equal(5, json.GetProperty("nullableCount").GetInt32());
    }

    [Fact]
    public async Task FromHeader_NullableHeadersOmitted_BindsAsNull()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "13");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableName").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableCount").ValueKind);
    }

    [Fact]
    public async Task FromHeader_InvalidRequired_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "0");

        var response = await _client.SendAsync(request, Ct);

        await AssertValidationProblem(response, "X-Count");
    }

    [Fact]
    public async Task FromHeader_InvalidNullable_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "13");
        request.Headers.Add("X-Nullable-Count", "0");

        var response = await _client.SendAsync(request, Ct);

        await AssertValidationProblem(response, "X-Nullable-Count");
    }

    [Fact]
    public async Task FromHeader_MissingRequiredHeader_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Count", "1");

        var response = await _client.SendAsync(request, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── [FromForm] ───────────────────────────────────────────────────────

    [Fact]
    public async Task FromForm_AllValuesPresent_BindsBothRequiredAndNullable()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "Eve",
            ["nullableName"] = "Eve2",
            ["count"] = "21",
            ["nullableCount"] = "5",
            ["email"] = "eve@example.com",
            ["nullableEmail"] = "eve2@example.com",
        });
        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Eve", json.GetProperty("name").GetString());
        Assert.Equal("Eve2", json.GetProperty("nullableName").GetString());
        Assert.Equal(21, json.GetProperty("count").GetInt32());
        Assert.Equal(5, json.GetProperty("nullableCount").GetInt32());
        Assert.Equal("eve@example.com", json.GetProperty("email").GetString());
        Assert.Equal("eve2@example.com", json.GetProperty("nullableEmail").GetString());
    }

    [Fact]
    public async Task FromForm_NullablesOmitted_BindsAsNull()
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
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableName").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableCount").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableEmail").ValueKind);
    }

    [Theory]
    [InlineData("name", "")]
    [InlineData("count", "0")]
    [InlineData("count", "not-a-number")]
    [InlineData("email", "not-an-email")]
    public async Task FromForm_InvalidRequired_Returns400(string field, string badValue)
    {
        var pairs = new Dictionary<string, string>
        {
            ["name"] = "Eve",
            ["count"] = "21",
            ["email"] = "eve@example.com",
        };
        pairs[field] = badValue;

        using var content = new FormUrlEncodedContent(pairs);
        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        await AssertValidationProblem(response, field);
    }

    [Theory]
    [InlineData("nullableCount", "0")]
    [InlineData("nullableEmail", "not-an-email")]
    public async Task FromForm_NonEmptyInvalidNullable_Returns400(string field, string badValue)
    {
        var pairs = new Dictionary<string, string>
        {
            ["name"] = "Eve",
            ["count"] = "21",
            ["email"] = "eve@example.com",
            [field] = badValue,
        };

        using var content = new FormUrlEncodedContent(pairs);
        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        await AssertValidationProblem(response, field);
    }

    [Fact]
    public async Task FromForm_EmptyNullable_BindsAsNull()
    {
        // Same NullableConverter quirk as FromQuery: empty form value for a
        // nullable type binds to null without invoking TryParse.
        var pairs = new Dictionary<string, string>
        {
            ["name"] = "Eve",
            ["count"] = "21",
            ["email"] = "eve@example.com",
            ["nullableName"] = "",
        };

        using var content = new FormUrlEncodedContent(pairs);
        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableName").ValueKind);
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
