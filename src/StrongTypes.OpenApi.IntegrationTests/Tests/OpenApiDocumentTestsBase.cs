using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using StrongTypes.OpenApi.IntegrationTests.Helpers;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

/// <summary>
/// The OpenAPI spec contract every strong-type wrapper must satisfy, regardless
/// of which generator produced the document. The Microsoft.AspNetCore.OpenApi
/// and Swashbuckle pipelines emit the same logical schema (modulo formatting,
/// ordering, and component naming); a single shared assertion suite pins the
/// shape and is run twice — once per concrete subclass — so any divergence
/// shows up as a per-generator failure.
///
/// The suite is split into feature-scoped partials (Strings, Numerics,
/// Collections, Composition, Annotations, Components) that all compile into
/// this one type — so each subclass still inherits every test, regardless of
/// which file declared it.
/// </summary>
public abstract partial class OpenApiDocumentTestsBase(HttpClient client) : IDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => client.Dispose();

    protected abstract string DocumentUrl { get; }

    /// <summary>
    /// The OpenAPI spec version the pipeline under test emits. Exclusive-bound
    /// helpers in <see cref="Helpers.ExclusiveBounds"/> pin the version's
    /// encoding strictly: 3.0 must emit <c>minimum: &lt;n&gt;</c> +
    /// <c>exclusiveMinimum: true</c> (boolean pair), 3.1 must emit
    /// <c>exclusiveMinimum: &lt;n&gt;</c> (numeric). Cross-version output
    /// fails the test rather than silently passing.
    /// </summary>
    protected abstract OpenApiVersion Version { get; }

    // Per-feature flags below name a specific keyword the underlying
    // pipeline doesn't natively write for the matching annotation, even
    // on a primitive-typed property. The corresponding tests run on every
    // pipeline; when the flag is set, they assert the keyword is *absent*
    // rather than skipping — so the suite documents the framework's
    // actual behaviour and catches it if the framework starts honouring
    // the annotation. Each flag is set in the per-pipeline subclass.
    protected virtual bool IsEmailStringFormatBroken => false;

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
