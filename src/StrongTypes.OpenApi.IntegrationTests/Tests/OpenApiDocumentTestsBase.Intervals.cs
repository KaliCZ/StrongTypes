using System.Text.Json;
using StrongTypes.OpenApi.IntegrationTests.Helpers;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

// Endpoint nullability encoding differs across pipelines and OpenAPI versions, so these
// assert the schema structurally instead of pinning one pipeline's literal JSON.
public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task FiniteInterval_Renders_As_Object_With_Both_Endpoints_Required_And_NonNullable()
    {
        var value = await IntervalValueSchema("/interval-entities/closed");
        AssertInlineSchema(value);
        AssertIntervalObject(value, "Start", "End");
        AssertIntegerEndpoint(Property(value, "Start"), nullable: false);
        AssertIntegerEndpoint(Property(value, "End"), nullable: false);
    }

    [Fact]
    public async Task Interval_Renders_With_Both_Endpoints_Nullable()
    {
        var value = await IntervalValueSchema("/interval-entities/open");
        AssertIntervalObject(value);   // neither endpoint required
        AssertIntegerEndpoint(Property(value, "Start"), nullable: true);
        AssertIntegerEndpoint(Property(value, "End"), nullable: true);
    }

    [Fact]
    public async Task IntervalFrom_Renders_With_NonNullable_Start_And_Nullable_End()
    {
        var value = await IntervalValueSchema("/interval-entities/from");
        AssertIntervalObject(value, "Start");
        AssertIntegerEndpoint(Property(value, "Start"), nullable: false);
        AssertIntegerEndpoint(Property(value, "End"), nullable: true);
    }

    [Fact]
    public async Task IntervalUntil_Renders_With_Nullable_Start_And_NonNullable_End()
    {
        var value = await IntervalValueSchema("/interval-entities/until");
        AssertIntervalObject(value, "End");
        AssertIntegerEndpoint(Property(value, "Start"), nullable: true);
        AssertIntegerEndpoint(Property(value, "End"), nullable: false);
    }

    [Theory]
    [InlineData("/interval-entities/closed", "Start", "End")]
    [InlineData("/interval-entities/open")]
    [InlineData("/interval-entities/from", "Start")]
    [InlineData("/interval-entities/until", "End")]
    public async Task Interval_BoundFlags_Render_As_Optional_Booleans_With_No_Default_On_Every_Variant(
        string path, params string[] requiredEndpoints)
    {
        var value = await IntervalValueSchema(path);
        AssertIntervalObject(value, requiredEndpoints);

        AssertBoundFlag(Property(value, "StartInclusive"));
        AssertBoundFlag(Property(value, "EndInclusive"));

        var required = RequiredSet(value);
        Assert.DoesNotContain("StartInclusive", required);
        Assert.DoesNotContain("EndInclusive", required);
    }

    [Fact]
    public async Task Nullable_Interval_Property_Wraps_The_Same_Interval_Object()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/interval-entities/closed"));
        var nullableValue = Resolve(doc, UnwrapNullableProperty(Property(body, "nullableValue"), Version));
        AssertIntervalObject(nullableValue, "Start", "End");
        AssertIntegerEndpoint(Property(nullableValue, "Start"), nullable: false);
        AssertIntegerEndpoint(Property(nullableValue, "End"), nullable: false);
    }

    private async Task<JsonElement> IntervalValueSchema(string path)
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, path));
        return Resolve(doc, Property(body, "value"));
    }

    private static void AssertIntervalObject(JsonElement schema, params string[] requiredEndpoints)
    {
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.Equal(requiredEndpoints.ToHashSet(), RequiredSet(schema));
    }

    // An all-optional variant lists nothing required; the keyword may then be
    // omitted entirely, so treat absent as the empty set.
    private static HashSet<string> RequiredSet(JsonElement schema)
        => schema.TryGetProperty("required", out var r)
            ? r.EnumerateArray().Select(e => e.GetString()!).ToHashSet()
            : [];

    private static void AssertBoundFlag(JsonElement schema)
    {
        Assert.Equal("boolean", schema.GetProperty("type").GetString());
        Assert.False(schema.TryGetProperty("default", out _), "inclusivity-on-omission is converter-dependent, so the schema must not pin a default");
    }

    // Tolerates every integer encoding the pipelines emit: Swashbuckle's
    // type:"integer", Microsoft 3.1's type:["integer","string"(,"null")], and
    // Microsoft 3.0's pattern-only plain-int form (no type keyword at all).
    // `format: int32` is the one marker present on all of them. Nullability is
    // read from whichever marker the version uses (nullable:true vs a "null"
    // member of the type array).
    private void AssertIntegerEndpoint(JsonElement schema, bool nullable)
    {
        Assert.Equal("int32", schema.GetProperty("format").GetString());

        bool nullableEncoded;
        if (schema.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.Array)
        {
            var types = type.EnumerateArray().Select(t => t.GetString()).ToArray();
            Assert.Contains("integer", types);
            nullableEncoded = types.Contains("null");
        }
        else
        {
            if (type.ValueKind == JsonValueKind.String)
                Assert.Equal("integer", type.GetString());
            else
                Assert.True(schema.TryGetProperty("pattern", out _), "plain-int form must carry a pattern when type is absent");
            nullableEncoded = schema.TryGetProperty("nullable", out var n) && n.ValueKind == JsonValueKind.True;
        }

        // A 3.1 document must not use the 3.0 nullable:true marker, and vice versa.
        if (Version == OpenApiVersion.V3_1)
            Assert.False(schema.TryGetProperty("nullable", out _), "3.1 must not emit the nullable keyword");

        Assert.Equal(nullable, nullableEncoded);
    }
}
