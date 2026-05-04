using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;
using static StrongTypes.Api.IntegrationTests.Tests.BindingTestAsserts;

namespace StrongTypes.Api.IntegrationTests.Tests;

/// <summary>
/// Verifies that strong types round-trip through MVC's <c>[FromHeader]</c>
/// model binding for both required and nullable variants of the wrapped
/// types, and that invalid header values produce a
/// <c>ValidationProblemDetails</c> 400.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class HeaderBindingTests(TestWebApplicationFactory factory) : IDisposable
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task FromHeader_AllValuesPresent_BindsBothRequiredAndNullable()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Nullable-Name", "Dana2");
        request.Headers.Add("X-Count", "13");
        request.Headers.Add("X-Nullable-Count", "5");
        request.Headers.Add("X-Digit", "7");
        request.Headers.Add("X-Nullable-Digit", "3");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Dana", json.GetProperty("name").GetString());
        Assert.Equal("Dana2", json.GetProperty("nullableName").GetString());
        Assert.Equal(13, json.GetProperty("count").GetInt32());
        Assert.Equal(5, json.GetProperty("nullableCount").GetInt32());
        Assert.Equal(7, json.GetProperty("digit").GetInt32());
        Assert.Equal(3, json.GetProperty("nullableDigit").GetInt32());
    }

    [Fact]
    public async Task FromHeader_NullableHeadersOmitted_BindsAsNull()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "13");
        request.Headers.Add("X-Digit", "7");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("Dana", json.GetProperty("name").GetString());
        Assert.Equal(13, json.GetProperty("count").GetInt32());
        Assert.Equal(7, json.GetProperty("digit").GetInt32());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableName").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableCount").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableDigit").ValueKind);
    }

    [Fact]
    public async Task FromHeader_InvalidRequired_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "0");
        request.Headers.Add("X-Digit", "7");

        var response = await _client.SendAsync(request, Ct);

        await AssertValidationProblem(response, "X-Count");
    }

    [Fact]
    public async Task FromHeader_InvalidNullable_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "13");
        request.Headers.Add("X-Digit", "7");
        request.Headers.Add("X-Nullable-Count", "0");

        var response = await _client.SendAsync(request, Ct);

        await AssertValidationProblem(response, "X-Nullable-Count");
    }

    [Fact]
    public async Task FromHeader_MissingRequiredHeader_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Count", "1");
        request.Headers.Add("X-Digit", "7");

        var response = await _client.SendAsync(request, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FromHeader_EmptyNullableHeader_ShortCircuitsToNull()
    {
        // Confirms the same NullableConverter quirk applies to headers as
        // it does to query / form: a present-but-empty value on a nullable
        // parsable type binds to null without invoking TryParse.
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "13");
        request.Headers.Add("X-Digit", "7");
        request.Headers.TryAddWithoutValidation("X-Nullable-Name", "");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("nullableName").ValueKind);
    }

    [Fact]
    public async Task FromHeader_InvalidDigit_Returns400()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header");
        request.Headers.Add("X-Name", "Dana");
        request.Headers.Add("X-Count", "13");
        request.Headers.Add("X-Digit", "12");

        var response = await _client.SendAsync(request, Ct);

        await AssertValidationProblem(response, "X-Digit");
    }

    [Fact]
    public async Task FromHeader_NonEmptyEnumerableAndMaybe_DoNotBindOutOfTheBox()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/binding-probe/header-unsupported");
        request.Headers.Add("X-Tags", "alpha");
        request.Headers.Add("X-Display-Name", "Ada");

        var response = await _client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
