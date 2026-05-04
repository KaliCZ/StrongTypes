using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;
using static StrongTypes.Api.IntegrationTests.Tests.BindingTestAsserts;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies that strong types round-trip through MVC's query-string model
/// binding — both <c>[FromQuery]</c> and the minimal-API-style implicit
/// query binding — and that invalid input lands in the
/// <see cref="HttpStatusCode.BadRequest"/> branch with a
/// <c>ValidationProblemDetails</c> payload.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class QueryBindingTests(TestWebApplicationFactory factory) : IDisposable
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
        Assert.Equal("Alice", json.GetProperty("name").GetString());
        Assert.Equal(42, json.GetProperty("count").GetInt32());
        Assert.Equal(7, json.GetProperty("digit").GetInt32());
        Assert.Equal("alice@example.com", json.GetProperty("email").GetString());
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

    [Theory]
    [InlineData("count=1&digit=0&email=alice@example.com", "name")]
    [InlineData("name=Alice&count=1&digit=0", "email")]
    public async Task FromQuery_MissingRequiredReferenceType_Returns400ProblemDetails(string query, string expectedField)
    {
        // Reference-type strong types (NonEmptyString, Email) produce a model
        // binding error for an omitted query parameter — there's no value to
        // bind and null isn't assignable to the non-nullable parameter, so MVC
        // surfaces a ValidationProblemDetails entry keyed by the parameter name.
        var response = await _client.GetAsync($"/binding-probe/query?{query}", Ct);

        await AssertValidationProblem(response, expectedField);
    }

    [Theory]
    [InlineData("name=Alice&digit=0&email=alice@example.com")]
    [InlineData("name=Alice&count=1&email=alice@example.com")]
    public async Task FromQuery_MissingRequiredValueType_BindsToDefault(string query)
    {
        // Value-type strong types (Positive<int>, Digit are structs) silently
        // bind to default(T) when the query parameter is omitted — MVC's
        // TryParseModelBinder doesn't run for a missing source value, and no
        // [BindRequired] attribute is present, so the action sees an
        // invariant-violating default rather than a 400. Documented here so
        // the contract is explicit; fixing this would need a custom binder
        // that flags missing required structs.
        var response = await _client.GetAsync($"/binding-probe/query?{query}", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Implicit query binding (no [From*] attribute) ────────────────────

    [Fact]
    public async Task ImplicitQuery_ValidValues_Binds()
    {
        var response = await _client.GetAsync(
            "/binding-probe/query-implicit?name=Bob&nullableName=Carol&count=7&nullableCount=3", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Bob", json.GetProperty("name").GetString());
        Assert.Equal("Carol", json.GetProperty("nullableName").GetString());
        Assert.Equal(7, json.GetProperty("count").GetInt32());
        Assert.Equal(3, json.GetProperty("nullableCount").GetInt32());
    }

    [Fact]
    public async Task ImplicitQuery_Invalid_Returns400()
    {
        var response = await _client.GetAsync("/binding-probe/query-implicit?name=Bob&count=0", Ct);
        await AssertValidationProblem(response, "count");
    }

    [Fact]
    public async Task FromQuery_NonEmptyEnumerableAndMaybe_DoNotBindOutOfTheBox()
    {
        var response = await _client.GetAsync(
            "/binding-probe/query-unsupported?tags=alpha&tags=beta&displayName=Ada", Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
