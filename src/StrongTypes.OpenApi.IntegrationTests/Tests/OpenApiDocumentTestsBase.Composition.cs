using StrongTypes.OpenApi.IntegrationTests.Helpers;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;

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
        // so the schema must describe an object with a non-required Value property —
        // a deep-equal against the literal shape catches any spurious `required: ["Value"]`
        // alongside the rest of the wrapper's wire form.
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/positive-int-entities/{id}", method: "patch"));
        var nullableValue = Resolve(doc, UnwrapNullableProperty(Property(body, "nullableValue"), Version));

        AssertJsonEquals(nullableValue, Version == OpenApiVersion.V3_1
            ? """{"type":"object","properties":{"Value":{"type":"integer","format":"int32","exclusiveMinimum":0}}}"""
            : """{"type":"object","properties":{"Value":{"type":"integer","format":"int32","minimum":0,"exclusiveMinimum":true}}}""");
    }

    [Fact]
    public async Task Maybe_Of_Positive_Int_Carries_Inner_Minimum_Zero_And_ExclusiveMinimum()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybePositiveInt"));

        AssertJsonEquals(maybe, Version == OpenApiVersion.V3_1
            ? """{"type":"object","properties":{"Value":{"type":"integer","format":"int32","exclusiveMinimum":0}}}"""
            : """{"type":"object","properties":{"Value":{"type":"integer","format":"int32","minimum":0,"exclusiveMinimum":true}}}""");
    }

    [Fact]
    public async Task Maybe_Of_NonEmptyString_Carries_Inner_MinLength_1()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybeNonEmptyString"));

        AssertJsonEquals(maybe, """{"type":"object","properties":{"Value":{"type":"string","minLength":1}}}""");
    }

    [Fact]
    public async Task Maybe_Of_NonEmptyEnumerable_Carries_Inner_MinItems_And_Element_Bound()
    {
        var doc = await GetDocumentAsync();
        var body = FollowRef(doc, RequestSchema(doc, "/nested-strong-types"));
        var maybe = Resolve(doc, Property(body, "maybeNonEmptyStringArray"));

        AssertJsonEquals(maybe, """{"type":"object","properties":{"Value":{"type":"array","minItems":1,"items":{"type":"string","minLength":1}}}}""");
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
        AssertJsonEquals(maybe, Version == OpenApiVersion.V3_1
            ? """{"type":"object","properties":{"Value":{"type":"integer","format":"int32","exclusiveMinimum":0}}}"""
            : """{"type":"object","properties":{"Value":{"type":"integer","format":"int32","minimum":0,"exclusiveMinimum":true}}}""");
    }
}
