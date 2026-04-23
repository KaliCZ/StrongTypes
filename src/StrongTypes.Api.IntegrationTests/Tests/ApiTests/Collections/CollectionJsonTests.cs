using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests.ApiTests.Collections;

/// <summary>
/// End-to-end tests for the JSON wire contract of collection-shaped request/response
/// DTOs. Every test POSTs a JSON body to a round-trip endpoint and inspects the
/// response — this exercises <em>both</em> halves of the JSON pipeline (STJ
/// deserialization + ASP.NET Core validation on the way in, and STJ serialization
/// on the way out) against a DTO wired with a deliberately broad matrix of property
/// shapes:
/// <list type="bullet">
///   <item><description><c>IEnumerable&lt;T&gt;</c> — vanilla collection, no invariant.</description></item>
///   <item><description><c>IEnumerable&lt;T?&gt;</c> — vanilla collection, element may be null.</description></item>
///   <item><description><c>NonEmptyEnumerable&lt;T&gt;</c> — StrongTypes collection, count ≥ 1.</description></item>
///   <item><description><c>NonEmptyEnumerable&lt;T?&gt;</c> — StrongTypes collection, count ≥ 1, element may be null.</description></item>
/// </list>
/// Element types cover the four categories: plain value (<c>int</c>), strong value
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
    // int — plain value type. Element null is only reachable for T? = int?.
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
        // IEnumerable<T> has no count invariant — an empty array is valid on the wire.
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
        // STJ can't assign null to an int, so deserializing [null] into IEnumerable<int>
        // fails at the element level and the controller returns 400.
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
    // Positive<int> — strong value type. Element null reachable for T? only.
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
        // Even in the nullable slot, a non-null value must still satisfy Positive<int>'s
        // invariant — null is a legit absence, 0 is a rule violation.
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
        // Positive<int> is a struct, so STJ rejects JSON null for the element type.
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
    // string — plain reference type. Element null is the interesting case:
    // NRT annotations erase at runtime, so we document the observed behavior.
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
        // Known limitation: NRT annotations erase at runtime, and ASP.NET Core's
        // nullable-annotation validator doesn't recurse into generic type arguments
        // — so `[null]` for `IEnumerable<string>` deserializes successfully and the
        // list ends up containing a null reference. This mirrors what plain C# without
        // StrongTypes does: `var l = new List<string>(); l.Add(null!);` compiles fine.
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
        // Same caveat as String_NullElementInNonNullableIEnumerable_PassesThrough:
        // the library can't distinguish NonEmptyEnumerable<string> from
        // NonEmptyEnumerable<string?> at runtime, so it accepts nulls. Consistent
        // with what plain C# allows, but worth documenting as a known gap.
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
    // NonEmptyString — strong reference type. Same null-erasure caveat applies,
    // but this is where it stings most: a null in the list defeats the type's
    // "non-null" guarantee. Flagged below where it occurs.
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
        // NonEmptyString's own converter rejects whitespace — the JsonException bubbles
        // up through the collection converter and ASP.NET turns it into a 400.
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
        // Nullable slot explicitly allows null — this is the intended use.
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
        // ⚠ Known gap: `IEnumerable<NonEmptyString>` with [null] deserializes to a list
        // containing a null reference, even though NonEmptyString's type contract is
        // "non-null". The NonEmptyString converter returns null for JSON null (by design,
        // to support NonEmptyString?), NRT annotations erase at runtime so the library
        // can't distinguish `NonEmptyString` from `NonEmptyString?`, and ASP.NET's
        // nullable validation doesn't walk into generic type arguments. Result: the
        // null element sails through.
        //
        // This is consistent with what plain C# without StrongTypes allows — the NRT
        // annotation is advisory at runtime — but it is genuinely weaker than what
        // a consumer reading `NonEmptyEnumerable<NonEmptyString>` would expect.
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
        // ⚠ Same gap as NonEmptyString_NullElementInNonNullableIEnumerable_PassesThrough_KnownGap.
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
