using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.ExclusiveBounds;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaWalk;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

// Transitive composition — when one strong type wraps another, the
// outer transformer must not swallow the inner's bounds. A
// Maybe<Positive<int>> needs minimum:0/exclusiveMinimum:true on its
// inner Value, not just type:integer; a Maybe<NonEmptyString> needs
// minLength:1; a NonEmptyEnumerable<Maybe<…>> needs both the array
// bound and the Maybe wrapper with its own inner bound.
public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task Maybe_T_Renders_As_Wrapper_Object_With_Value_Property()
    {
        // The PATCH request for a numeric entity carries `Maybe<T>? NullableValue`.
        // The converter writes {"Value": x} or {"Value": null} and reads {} as None,
        // so the schema must describe an object with a non-required Value property.
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/positive-int-entities/{id}", method: "patch"));
        var nullableValue = Resolve(doc, UnwrapNullableProperty(Property(body, "nullableValue"), Version));

        Assert.Equal("object", StringOrNull(nullableValue, "type"));
        Assert.True(nullableValue.TryGetProperty("properties", out var props));
        Assert.True(props.TryGetProperty("Value", out var inner));

        AssertInlineSchema(inner);
        Assert.Equal("integer", StringOrNull(inner, "type"));

        // Value is not listed under `required` — that's how the converter
        // accepts `{}` as the None case.
        var required = nullableValue.TryGetProperty("required", out var r)
            ? r.EnumerateArray().Select(e => e.GetString()).ToArray()
            : [];
        Assert.DoesNotContain("Value", required);
    }

    [Fact]
    public async Task Maybe_Of_Positive_Int_Carries_Inner_Minimum_Zero_And_ExclusiveMinimum()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybePositiveInt"));

        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = maybe.GetProperty("properties").GetProperty("Value");
        AssertInlineSchema(inner);
        Assert.Equal("integer", StringOrNull(inner, "type"));
        Assert.Equal("int32", StringOrNull(inner, "format"));
        AssertExclusiveLowerBound(inner, 0m, Version);
    }

    [Fact]
    public async Task Maybe_Of_NonEmptyString_Carries_Inner_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybeNonEmptyString"));

        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = maybe.GetProperty("properties").GetProperty("Value");
        AssertInlineSchema(inner);
        Assert.Equal("string", StringOrNull(inner, "type"));
        Assert.Equal(1, IntOrNull(inner, "minLength"));
    }

    [Fact]
    public async Task Maybe_Of_NonEmptyEnumerable_Carries_Inner_MinItems_And_Element_Bound()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybeNonEmptyStringArray"));

        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = maybe.GetProperty("properties").GetProperty("Value");
        AssertInlineSchema(inner);
        Assert.Equal("array", StringOrNull(inner, "type"));
        Assert.Equal(1, IntOrNull(inner, "minItems"));

        var items = inner.GetProperty("items");
        AssertInlineSchema(items);
        Assert.Equal("string", StringOrNull(items, "type"));
        Assert.Equal(1, IntOrNull(items, "minLength"));
    }

    [Fact]
    public async Task NonEmptyEnumerable_Of_Maybe_Of_Positive_Int_Carries_Every_Bound()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var array = Property(body, "nonEmptyArrayOfMaybePositiveInt");

        AssertInlineSchema(array);
        Assert.Equal("array", StringOrNull(array, "type"));
        Assert.Equal(1, IntOrNull(array, "minItems"));

        var maybe = Resolve(doc, array.GetProperty("items"));
        Assert.Equal("object", StringOrNull(maybe, "type"));

        var inner = maybe.GetProperty("properties").GetProperty("Value");
        AssertInlineSchema(inner);
        Assert.Equal("integer", StringOrNull(inner, "type"));
        Assert.Equal("int32", StringOrNull(inner, "format"));
        AssertExclusiveLowerBound(inner, 0m, Version);
    }
}
