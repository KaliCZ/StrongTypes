using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests.ApiTests.Collections;

/// <summary>
/// POST round-trips so both halves of the JSON pipeline are exercised (STJ deserialization +
/// validation in, STJ serialization out), across the collection-shape matrix on the DTOs with
/// one element type per category: plain value (<c>int</c>), strong value
/// (<c>Positive&lt;int&gt;</c>), plain reference (<c>string</c>), strong reference
/// (<c>NonEmptyString</c>).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CollectionJsonTests(TestWebApplicationFactory factory) : IDisposable
{
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string IntEndpoint = "/collections/int";
    private const string PositiveIntEndpoint = "/collections/positive-int";
    private const string StringEndpoint = "/collections/string";
    private const string NonEmptyStringEndpoint = "/collections/non-empty-string";

    public void Dispose() => _client.Dispose();

    private async Task<JsonElement> PostOk(string endpoint, object body)
    {
        var response = await _client.PostAsJsonAsync(endpoint, body, Ct);
        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected success but got {(int)response.StatusCode} {response.StatusCode}: {await response.Content.ReadAsStringAsync(Ct)}");
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    private async Task<HttpStatusCode> PostStatus(string endpoint, object body)
    {
        var response = await _client.PostAsJsonAsync(endpoint, body, Ct);
        return response.StatusCode;
    }

    private static int[] IntArray(JsonElement parent, string property) =>
        parent.GetProperty(property).EnumerateArray().Select(e => e.GetInt32()).ToArray();

    private static int?[] NullableIntArray(JsonElement parent, string property) =>
        parent.GetProperty(property).EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.Null ? (int?)null : e.GetInt32())
            .ToArray();

    private static string[] StringArray(JsonElement parent, string property) =>
        parent.GetProperty(property).EnumerateArray().Select(e => e.GetString()!).ToArray();

    private static string?[] NullableStringArray(JsonElement parent, string property) =>
        parent.GetProperty(property).EnumerateArray()
            .Select(e => e.ValueKind == JsonValueKind.Null ? null : e.GetString())
            .ToArray();

    // ───────────────────────────────────────────────────────────────────
    // int — plain value type
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Int_AllValid_RoundTrips()
    {
        var echoed = await PostOk(IntEndpoint, new
        {
            enumerable = new[] { 1, 2, 3 },
            enumerableNullable = new int?[] { 1, null, 3 },
            nonEmpty = new[] { 10, 20 },
            nonEmptyNullable = new int?[] { null, 5 }
        });

        Assert.Equal(new[] { 1, 2, 3 }, IntArray(echoed, "enumerable"));
        Assert.Equal(new int?[] { 1, null, 3 }, NullableIntArray(echoed, "enumerableNullable"));
        Assert.Equal(new[] { 10, 20 }, IntArray(echoed, "nonEmpty"));
        Assert.Equal(new int?[] { null, 5 }, NullableIntArray(echoed, "nonEmptyNullable"));
    }

    [Fact]
    public async Task Int_IEnumerableEmpty_Allowed()
    {
        var echoed = await PostOk(IntEndpoint, new
        {
            enumerable = Array.Empty<int>(),
            enumerableNullable = Array.Empty<int?>(),
            nonEmpty = new[] { 1 },
            nonEmptyNullable = new int?[] { 1 }
        });

        Assert.Empty(echoed.GetProperty("enumerable").EnumerateArray());
        Assert.Empty(echoed.GetProperty("enumerableNullable").EnumerateArray());
    }

    [Fact]
    public async Task Int_NonEmptyEmpty_Returns400()
    {
        var status = await PostStatus(IntEndpoint, new
        {
            enumerable = new[] { 1 },
            enumerableNullable = new int?[] { 1 },
            nonEmpty = Array.Empty<int>(),
            nonEmptyNullable = new int?[] { 1 }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task Int_NonEmptyNullableEmpty_Returns400()
    {
        var status = await PostStatus(IntEndpoint, new
        {
            enumerable = new[] { 1 },
            enumerableNullable = new int?[] { 1 },
            nonEmpty = new[] { 1 },
            nonEmptyNullable = Array.Empty<int?>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task Int_NullElementInNonNullableIEnumerable_Returns400()
    {
        var status = await PostStatus(IntEndpoint, new
        {
            enumerable = new int?[] { 1, null, 3 },
            enumerableNullable = new int?[] { 1 },
            nonEmpty = new[] { 1 },
            nonEmptyNullable = new int?[] { 1 }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task Int_NullElementInNonNullableNonEmpty_Returns400()
    {
        var status = await PostStatus(IntEndpoint, new
        {
            enumerable = new[] { 1 },
            enumerableNullable = new int?[] { 1 },
            nonEmpty = new int?[] { 1, null, 3 },
            nonEmptyNullable = new int?[] { 1 }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    // ───────────────────────────────────────────────────────────────────
    // Positive<int> — strong value type
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Positive_AllValid_RoundTrips()
    {
        var echoed = await PostOk(PositiveIntEndpoint, new
        {
            enumerable = new[] { 1, 2, 3 },
            enumerableNullable = new int?[] { 1, null, 3 },
            nonEmpty = new[] { 10, 20 },
            nonEmptyNullable = new int?[] { null, 5 }
        });

        Assert.Equal(new[] { 1, 2, 3 }, IntArray(echoed, "enumerable"));
        Assert.Equal(new int?[] { 1, null, 3 }, NullableIntArray(echoed, "enumerableNullable"));
        Assert.Equal(new[] { 10, 20 }, IntArray(echoed, "nonEmpty"));
        Assert.Equal(new int?[] { null, 5 }, NullableIntArray(echoed, "nonEmptyNullable"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Positive_InvalidElementInIEnumerable_Returns400(int invalid)
    {
        var status = await PostStatus(PositiveIntEndpoint, new
        {
            enumerable = new[] { 1, invalid, 3 },
            enumerableNullable = new int?[] { 1 },
            nonEmpty = new[] { 1 },
            nonEmptyNullable = new int?[] { 1 }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Positive_InvalidElementInNonEmpty_Returns400(int invalid)
    {
        var status = await PostStatus(PositiveIntEndpoint, new
        {
            enumerable = new[] { 1 },
            enumerableNullable = new int?[] { 1 },
            nonEmpty = new[] { 1, invalid, 3 },
            nonEmptyNullable = new int?[] { 1 }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task Positive_InvalidElementInNullableSlot_Returns400()
    {
        var status = await PostStatus(PositiveIntEndpoint, new
        {
            enumerable = new[] { 1 },
            enumerableNullable = new int?[] { 1, 0, null },
            nonEmpty = new[] { 1 },
            nonEmptyNullable = new int?[] { 1 }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task Positive_NonEmptyEmpty_Returns400()
    {
        var status = await PostStatus(PositiveIntEndpoint, new
        {
            enumerable = new[] { 1 },
            enumerableNullable = new int?[] { 1 },
            nonEmpty = Array.Empty<int>(),
            nonEmptyNullable = new int?[] { 1 }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task Positive_NullElementInNonNullableIEnumerable_Returns400()
    {
        var status = await PostStatus(PositiveIntEndpoint, new
        {
            enumerable = new int?[] { 1, null, 3 },
            enumerableNullable = new int?[] { 1 },
            nonEmpty = new[] { 1 },
            nonEmptyNullable = new int?[] { 1 }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    // ───────────────────────────────────────────────────────────────────
    // string — plain reference type
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task String_AllValid_RoundTrips()
    {
        var echoed = await PostOk(StringEndpoint, new
        {
            enumerable = new[] { "a", "b", "c" },
            enumerableNullable = new string?[] { "a", null, "c" },
            nonEmpty = new[] { "x", "y" },
            nonEmptyNullable = new string?[] { null, "z" }
        });

        Assert.Equal(new[] { "a", "b", "c" }, StringArray(echoed, "enumerable"));
        Assert.Equal(new string?[] { "a", null, "c" }, NullableStringArray(echoed, "enumerableNullable"));
        Assert.Equal(new[] { "x", "y" }, StringArray(echoed, "nonEmpty"));
        Assert.Equal(new string?[] { null, "z" }, NullableStringArray(echoed, "nonEmptyNullable"));
    }

    [Fact]
    public async Task String_NonEmptyEmpty_Returns400()
    {
        var status = await PostStatus(StringEndpoint, new
        {
            enumerable = new[] { "x" },
            enumerableNullable = new string?[] { "x" },
            nonEmpty = Array.Empty<string>(),
            nonEmptyNullable = new string?[] { "x" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task String_NullElementInNonNullableIEnumerable_PassesThrough()
    {
        // Known limitation being pinned: NRT annotations erase at runtime and ASP.NET Core's
        // nullable validation does not recurse into generic type arguments, so the null sails through.
        var echoed = await PostOk(StringEndpoint, new
        {
            enumerable = new string?[] { "a", null, "c" },
            enumerableNullable = new string?[] { "x" },
            nonEmpty = new[] { "x" },
            nonEmptyNullable = new string?[] { "x" }
        });

        Assert.Equal(new string?[] { "a", null, "c" }, NullableStringArray(echoed, "enumerable"));
    }

    [Fact]
    public async Task String_NullElementInNonNullableNonEmpty_PassesThrough()
    {
        // Same erasure limitation as the IEnumerable case above.
        var echoed = await PostOk(StringEndpoint, new
        {
            enumerable = new[] { "x" },
            enumerableNullable = new string?[] { "x" },
            nonEmpty = new string?[] { "a", null, "c" },
            nonEmptyNullable = new string?[] { "x" }
        });

        Assert.Equal(new string?[] { "a", null, "c" }, NullableStringArray(echoed, "nonEmpty"));
    }

    // ───────────────────────────────────────────────────────────────────
    // NonEmptyString — strong reference type
    // ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NonEmptyString_AllValid_RoundTrips()
    {
        var echoed = await PostOk(NonEmptyStringEndpoint, new
        {
            enumerable = new[] { "a", "b", "c" },
            enumerableNullable = new string?[] { "a", null, "c" },
            nonEmpty = new[] { "x", "y" },
            nonEmptyNullable = new string?[] { null, "z" }
        });

        Assert.Equal(new[] { "a", "b", "c" }, StringArray(echoed, "enumerable"));
        Assert.Equal(new string?[] { "a", null, "c" }, NullableStringArray(echoed, "enumerableNullable"));
        Assert.Equal(new[] { "x", "y" }, StringArray(echoed, "nonEmpty"));
        Assert.Equal(new string?[] { null, "z" }, NullableStringArray(echoed, "nonEmptyNullable"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NonEmptyString_InvalidElementInIEnumerable_Returns400(string invalid)
    {
        var status = await PostStatus(NonEmptyStringEndpoint, new
        {
            enumerable = new[] { "a", invalid, "c" },
            enumerableNullable = new string?[] { "a" },
            nonEmpty = new[] { "a" },
            nonEmptyNullable = new string?[] { "a" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NonEmptyString_InvalidElementInNonEmpty_Returns400(string invalid)
    {
        var status = await PostStatus(NonEmptyStringEndpoint, new
        {
            enumerable = new[] { "a" },
            enumerableNullable = new string?[] { "a" },
            nonEmpty = new[] { "a", invalid, "c" },
            nonEmptyNullable = new string?[] { "a" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task NonEmptyString_NonEmptyEmpty_Returns400()
    {
        var status = await PostStatus(NonEmptyStringEndpoint, new
        {
            enumerable = new[] { "a" },
            enumerableNullable = new string?[] { "a" },
            nonEmpty = Array.Empty<string>(),
            nonEmptyNullable = new string?[] { "a" }
        });
        Assert.Equal(HttpStatusCode.BadRequest, status);
    }

    [Fact]
    public async Task NonEmptyString_NullInNullableSlot_RoundTrips()
    {
        var echoed = await PostOk(NonEmptyStringEndpoint, new
        {
            enumerable = new[] { "a" },
            enumerableNullable = new string?[] { "a", null, "c" },
            nonEmpty = new[] { "a" },
            nonEmptyNullable = new string?[] { null, "a" }
        });

        Assert.Equal(new string?[] { "a", null, "c" }, NullableStringArray(echoed, "enumerableNullable"));
        Assert.Equal(new string?[] { null, "a" }, NullableStringArray(echoed, "nonEmptyNullable"));
    }

    [Fact]
    public async Task NonEmptyString_NullElementInNonNullableIEnumerable_PassesThrough_KnownGap()
    {
        // Known gap being pinned: the converter maps JSON null through (by design, for
        // NonEmptyString?) and NRT erasure hides NonEmptyString vs NonEmptyString? at runtime,
        // so the null sails through the "non-null" contract.
        var echoed = await PostOk(NonEmptyStringEndpoint, new
        {
            enumerable = new string?[] { "a", null, "c" },
            enumerableNullable = new string?[] { "a" },
            nonEmpty = new[] { "a" },
            nonEmptyNullable = new string?[] { "a" }
        });

        Assert.Equal(new string?[] { "a", null, "c" }, NullableStringArray(echoed, "enumerable"));
    }

    [Fact]
    public async Task NonEmptyString_NullElementInNonNullableNonEmpty_PassesThrough_KnownGap()
    {
        // Same gap as NonEmptyString_NullElementInNonNullableIEnumerable_PassesThrough_KnownGap.
        var echoed = await PostOk(NonEmptyStringEndpoint, new
        {
            enumerable = new[] { "a" },
            enumerableNullable = new string?[] { "a" },
            nonEmpty = new string?[] { "a", null, "c" },
            nonEmptyNullable = new string?[] { "a" }
        });

        Assert.Equal(new string?[] { "a", null, "c" }, NullableStringArray(echoed, "nonEmpty"));
    }
}
