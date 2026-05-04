using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StrongTypes.AspNetCore.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.AspNetCore.IntegrationTests.Tests.BindingTests;

/// <summary>
/// Verifies the MVC binder shipped in <c>Kalicz.StrongTypes.AspNetCore</c>
/// for form-bound nullable Maybe values that preserve omitted vs empty vs
/// populated input.
/// </summary>
public sealed class MaybeBindingTests(AspNetCoreTestApiFactory factory) : IClassFixture<AspNetCoreTestApiFactory>, IDisposable
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Form_NullableMaybeWithValue_BindsToSome()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "FormName",
        });

        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("some", json.GetProperty("displayNameState").GetString());
        Assert.Equal("FormName", json.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Form_NullableMaybeOmitted_BindsToMissingState()
    {
        using var content = new FormUrlEncodedContent([]);

        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("missing", json.GetProperty("displayNameState").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("displayName").ValueKind);
    }

    [Fact]
    public async Task Form_EmptyNullableMaybe_BindsToNone()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["displayName"] = "",
        });

        var response = await _client.PostAsync("/binding-probe/form", content, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.Equal("none", json.GetProperty("displayNameState").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("displayName").ValueKind);
    }
}
