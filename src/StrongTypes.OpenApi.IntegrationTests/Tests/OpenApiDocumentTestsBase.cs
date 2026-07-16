using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using StrongTypes.OpenApi.IntegrationTests.Helpers;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

/// <summary>The shared OpenAPI shape-contract suite — see "OpenAPI integration tests" in testing.md.</summary>
public abstract partial class OpenApiDocumentTestsBase(HttpClient client) : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => client.Dispose();

    protected abstract string DocumentUrl { get; }

    protected abstract OpenApiVersion Version { get; }

    /// <summary>
    /// True when the pipeline drops <c>[EmailAddress]</c>'s <c>format: email</c> from string slots — the <c>Email</c>
    /// wrapper is independent and paints the format on every pipeline.
    /// </summary>
    protected virtual bool IsEmailAddressFormatIgnored => false;

    protected virtual bool IsPlainIntFormSchemaMissingType => true;

    private async Task<JsonElement> GetDocumentAsync()
    {
        var response = await client.GetAsync(DocumentUrl, Ct);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected success but got {(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync(Ct)}");
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    [Fact]
    public async Task Document_IsServed()
    {
        var doc = await GetDocumentAsync();
        Assert.Equal(JsonValueKind.Object, doc.ValueKind);
        Assert.True(doc.TryGetProperty("paths", out _));
    }
}
