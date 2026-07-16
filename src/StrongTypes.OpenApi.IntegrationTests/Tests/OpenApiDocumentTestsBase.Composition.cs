using StrongTypes.OpenApi.IntegrationTests.Helpers;
using Xunit;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.NullableUnwrap;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaNavigation;
using static StrongTypes.OpenApi.IntegrationTests.Helpers.SchemaValueReader;

namespace StrongTypes.OpenApi.IntegrationTests.Tests;

// When one strong type wraps another, the outer transformer must not swallow the inner's bounds.
public abstract partial class OpenApiDocumentTestsBase
{
    [Fact]
    public async Task Maybe_T_Renders_As_Wrapper_Object_With_Value_Property()
    {
        // The Maybe converter reads {} as None, so the schema must not require Value.
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
