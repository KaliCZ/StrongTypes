using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;
using static StrongTypes.Api.IntegrationTests.Tests.BindingTestAsserts;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies that strong types round-trip through MVC's <c>[FromForm]</c>
/// model binding for both required and nullable variants of the wrapped
/// types, and that invalid form fields produce a
/// <c>ValidationProblemDetails</c> 400.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class FormBindingTests(TestWebApplicationFactory factory) : IDisposable
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _client.Dispose();

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
        Assert.Equal("Eve", json.GetProperty("name").GetString());
        Assert.Equal(21, json.GetProperty("count").GetInt32());
        Assert.Equal("eve@example.com", json.GetProperty("email").GetString());
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
}
