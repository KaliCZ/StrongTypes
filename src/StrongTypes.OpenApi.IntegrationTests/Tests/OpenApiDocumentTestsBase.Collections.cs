using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ExclusiveBounds;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaWalk;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task NonEmptyEnumerable_Renders_As_Array_With_MinItems_And_Items_Schema()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/non-empty-string"));
        var nonEmpty = Property(body, "nonEmpty");

        AssertInlineSchema(nonEmpty);
        Assert.Equal("array", StringOrNull(nonEmpty, "type"));
        Assert.Equal(1, IntOrNull(nonEmpty, "minItems"));

        var items = nonEmpty.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task Enumerable_Of_NonEmptyString_Has_No_MinItems_But_String_Items()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/non-empty-string"));
        var enumerable = Property(body, "enumerable");

        AssertInlineSchema(enumerable);
        Assert.Equal("array", StringOrNull(enumerable, "type"));
        Assert.Null(IntOrNull(enumerable, "minItems"));

        var items = enumerable.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task NonEmptyEnumerable_Of_Positive_Int_Composes_With_Numeric_Transformer()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/positive-int"));
        var nonEmpty = Property(body, "nonEmpty");

        AssertInlineSchema(nonEmpty);
        Assert.Equal("array", StringOrNull(nonEmpty, "type"));
        Assert.Equal(1, IntOrNull(nonEmpty, "minItems"));

        var items = nonEmpty.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("integer", StringOrNull(items, "type"));
        Assert.Equal("int32", StringOrNull(items, "format"));
        AssertExclusiveLowerBound(items, 0m, Version);
    }

    // ───────────────────────────────────────────────────────────────────
    // Collection shapes — every CLR collection shape carrying a strong-type
    // element must expose `items` with the element's wire schema. The
    // `/collections/shapes` endpoint declares one property per shape, all
    // typed as `Positive<int>` so the items schema must carry
    // `exclusiveMinimum: 0`. Each shape is asserted independently so the
    // failure surface tells us which shapes the underlying pipeline (and
    // any items-backfill transformer it relies on) actually covers.
    // ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("asEnumerable")]
    [InlineData("asIList")]
    [InlineData("asIReadOnlyList")]
    [InlineData("asList")]
    [InlineData("asArray")]
    [InlineData("asNonEmpty")]
    [InlineData("asFrozenSet")]
    public async Task Collection_Shape_Of_Positive_Int_Renders_As_Array_With_Integer_Items(string propertyName)
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/shapes"));
        var array = Property(body, propertyName);

        AssertInlineSchema(array);
        Assert.Equal("array", StringOrNull(array, "type"));

        var items = array.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("integer", StringOrNull(items, "type"));
        Assert.Equal("int32", StringOrNull(items, "format"));
        AssertExclusiveLowerBound(items, 0m, Version);
    }

    // ───────────────────────────────────────────────────────────────────
    // Dictionary shapes — the wire form for a CLR dictionary keyed by a
    // primitive is an OpenAPI object with `additionalProperties`. Each
    // dictionary property below carries a `Positive<int>` value, so the
    // value-schema position must encode `exclusiveMinimum: 0`.
    // ───────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("asIDictionary")]
    [InlineData("asDictionaryIntKey")]
    [InlineData("asIReadOnlyDictionary")]
    [InlineData("asFrozenDictionary")]
    [InlineData("asSortedList")]
    public async Task Dictionary_Shape_Of_Positive_Int_Renders_With_Integer_AdditionalProperties(string propertyName)
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/collections/dictionary-shapes"));
        var dict = Property(body, propertyName);

        AssertInlineSchema(dict);
        Assert.Equal("object", StringOrNull(dict, "type"));

        Assert.True(dict.TryGetProperty("additionalProperties", out var values),
            "additionalProperties is missing on the dictionary schema");
        AssertInlineSchema(values);
        Assert.Equal("integer", StringOrNull(values, "type"));
        Assert.Equal("int32", StringOrNull(values, "format"));
        AssertExclusiveLowerBound(values, 0m, Version);
    }

    [Fact]
    public async Task Nullable_NonEmptyEnumerable_Of_NonEmptyString_Still_Renders_As_Array_With_String_Items()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        var nullableArray = UnwrapNullableProperty(Property(body, "nullableNonEmptyStringArray"), Version);

        AssertInlineSchema(nullableArray);
        Assert.Equal("array", StringOrNull(nullableArray, "type"));
        Assert.Equal(1, IntOrNull(nullableArray, "minItems"));

        var items = nullableArray.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task Nullable_NonEmptyEnumerable_Of_Positive_Int_Still_Renders_As_Array_With_Integer_Items()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nullable-strong-types"));
        var nullableArray = UnwrapNullableProperty(Property(body, "nullableNonEmptyPositiveIntArray"), Version);

        AssertInlineSchema(nullableArray);
        Assert.Equal("array", StringOrNull(nullableArray, "type"));
        Assert.Equal(1, IntOrNull(nullableArray, "minItems"));

        var items = nullableArray.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("integer", StringOrNull(items, "type"));
        Assert.Equal("int32", StringOrNull(items, "format"));
        AssertExclusiveLowerBound(items, 0m, Version);
    }
}
